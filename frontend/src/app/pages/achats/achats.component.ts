import { Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { DateRangeComponent } from '../../shared/date-range/date-range.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';
import { AuthService } from '../../core/services/auth.service';
import { Achat, Fournisseur } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-achats',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, PaginationComponent, FormsModule, DateRangeComponent],
  templateUrl: './achats.component.html',
})
export class AchatsComponent implements OnInit {
  @Input() hideTopbar = false;

  items: Achat[] = [];
  fournisseurs: Fournisseur[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  fournisseurFilter = '';
  dateFrom = '';
  dateTo = '';
  page = 1;
  pageSize = 10;
  total = 0;
  stats = { depenses: 0, count: 0, fournisseurs: 0, impayes: 0, nbImpayes: 0 };

  modalOpen = false;
  viewAchat: Achat | null = null;
  paiementMontant = 0;
  paiementMethode = 'Espece';
  paiementSaving = false;
  cancelSaving = false;

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  Math = Math;

  private searchTimer: any;

  constructor(private api: ApiService, private toast: ToastService, private exportSvc: ExportService, public router: Router, public auth: AuthService) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([this.load(), this.loadStats(), this.loadFournisseurs()]);
  }

  private async loadFournisseurs(): Promise<void> {
    try { this.fournisseurs = await this.api.fournisseursList(); } catch { /* ignore */ }
  }

  async loadStats(): Promise<void> {
    const now = new Date();
    const dateDebut = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
    const [monthRes, allRes] = await Promise.allSettled([
      this.api.achatsListPaged({ page: 1, pageSize: 500, dateDebut }),
      this.api.achatsListPaged({ page: 1, pageSize: 500 }),
    ]);
    const month = monthRes.status === 'fulfilled' ? monthRes.value.items : [];
    const all   = allRes.status === 'fulfilled'   ? allRes.value.items   : [];
    const fournIds = new Set(all.map(a => a.fournisseurId));
    const depenses = month.reduce((s, a) => s + a.montantTotal, 0);
    const impayes  = all.filter(a => a.statut !== 'Paye' && a.statut !== 'Annule').reduce((s, a) => s + a.reste, 0);
    this.stats = {
      depenses, count: month.length, fournisseurs: fournIds.size,
      impayes, nbImpayes: all.filter(a => a.statut !== 'Paye' && a.statut !== 'Annule' && a.reste > 0).length,
    };
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.achatsListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        statut: this.statusFilter || undefined,
        fournisseurId: this.fournisseurFilter ? Number(this.fournisseurFilter) : undefined,
        dateDebut: this.dateFrom || undefined,
        dateFin: this.dateTo || undefined,
      });
      this.items = result.items;
      this.total = result.totalCount;
    } finally { this.loading = false; }
  }

  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }
  onFilterChange(): void { this.page = 1; this.load(); }
  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }

  achatStatus(a: Achat): { label: string; cls: string } {
    if (a.statut === 'Paye') return { label: 'Payé', cls: 'paid' };
    if (a.statut === 'Partiel') return { label: 'Partiel', cls: 'partial' };
    if (a.statut === 'Annule') return { label: 'Annulé', cls: 'cancelled' };
    return { label: 'Crédit', cls: 'pending' };
  }

  resetFilters(): void {
    this.search = ''; this.statusFilter = ''; this.fournisseurFilter = ''; this.dateFrom = ''; this.dateTo = ''; this.page = 1;
    this.load();
  }

  openDetail(a: Achat): void {
    this.viewAchat = a; this.paiementMontant = 0; this.paiementMethode = 'Espece'; this.modalOpen = true;
  }

  async addPaiement(): Promise<void> {
    if (!this.viewAchat || this.paiementMontant <= 0) return;
    this.paiementSaving = true;
    try {
      await this.api.achatAddPaiement(this.viewAchat.id, { montant: this.paiementMontant, methode: this.paiementMethode });
      this.paiementMontant = 0;
      this.toast.notify('Paiement enregistré', 'success');
      this.modalOpen = false; this.viewAchat = null;
      await Promise.all([this.load(), this.loadStats()]);
    } catch { this.toast.notify('Erreur lors du paiement', 'error'); }
    finally { this.paiementSaving = false; }
  }

  async handleCancel(id: number): Promise<void> {
    if (!confirm('Annuler cet achat ? Le stock sera décrémenté des quantités reçues.')) return;
    this.cancelSaving = true;
    try {
      await this.api.achatCancel(id);
      this.toast.notify('Achat annulé — stock mis à jour', 'success');
      this.modalOpen = false; this.viewAchat = null;
      await Promise.all([this.load(), this.loadStats()]);
    } catch (e: any) {
      const msg = e?.error?.message || 'Erreur lors de l\'annulation';
      this.toast.notify(msg, 'error');
    } finally { this.cancelSaving = false; }
  }

  async exportExcel(): Promise<void> {
    try {
      const r = await this.api.achatsListPaged({ page: 1, pageSize: 2000 });
      this.exportSvc.exportAchats(r.items);
    } catch { this.toast.notify('Erreur export', 'error'); }
  }
}
