import { Component, OnInit, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { AchatsComponent } from '../achats/achats.component';
import { ChargesComponent } from '../charges/charges.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';

type DepensesTab = 'achats' | 'charges';

@Component({
  selector: 'app-depenses',
  standalone: true,
  imports: [TopbarComponent, AchatsComponent, ChargesComponent],
  templateUrl: './depenses.component.html',
})
export class DepensesComponent implements OnInit {
  activeTab: DepensesTab = 'achats';
  achatsCount = 0;
  chargesCount = 0;
  exportingExcel = false;

  @ViewChild(ChargesComponent) chargesRef?: ChargesComponent;

  constructor(
    private route: ActivatedRoute,
    public router: Router,
    private api: ApiService,
    private toast: ToastService,
    private exportSvc: ExportService,
  ) {}

  ngOnInit(): void {
    const tab = this.route.snapshot.queryParamMap.get('tab') as DepensesTab | null;
    if (tab === 'charges') this.activeTab = 'charges';
    this.loadCounts();
  }

  private async loadCounts(): Promise<void> {
    try {
      const [a, c] = await Promise.all([
        this.api.achatsListPaged({ page: 1, pageSize: 1 }).catch(() => ({ totalCount: 0 })),
        this.api.chargesListPaged({ page: 1, pageSize: 1 }).catch(() => ({ totalCount: 0 })),
      ]);
      this.achatsCount = a.totalCount;
      this.chargesCount = c.totalCount;
    } catch { /* ignore */ }
  }

  setTab(tab: DepensesTab): void {
    this.activeTab = tab;
    this.router.navigate([], { queryParams: { tab }, replaceUrl: true });
  }

  get tabTitle(): string {
    return this.activeTab === 'achats' ? 'Achats' : 'Charges';
  }

  get tabSubtitle(): string {
    if (this.activeTab === 'achats') {
      return this.achatsCount + ' achat' + (this.achatsCount > 1 ? 's' : '') + ' — Commandes fournisseurs';
    }
    return this.chargesCount + ' charge' + (this.chargesCount > 1 ? 's' : '') + ' — Gestion des dépenses';
  }

  get tabIcon(): string { return this.activeTab === 'achats' ? '📥' : '💸'; }

  get chargesGenerating(): boolean { return this.chargesRef?.generatingRecurrentes ?? false; }

  async exportExcel(): Promise<void> {
    this.exportingExcel = true;
    try {
      const r = await this.api.achatsListPaged({ page: 1, pageSize: 2000 });
      this.exportSvc.exportAchats(r.items);
    } catch { this.toast.notify('Erreur export', 'error'); }
    finally { this.exportingExcel = false; }
  }

  openCatModal(): void { this.chargesRef?.openNewCatModal(); }

  genererRecurrentes(): void { this.chargesRef?.genererRecurrentes(); }
}
