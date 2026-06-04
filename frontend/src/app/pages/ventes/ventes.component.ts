import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule, NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { DateRangeComponent } from '../../shared/date-range/date-range.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';
import { AuthService } from '../../core/services/auth.service';
import { Vente, Client } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus, statusInfo } from '../../core/utils/format';

type DateRange = 'today' | '7d' | '30d' | '12m' | 'all';

@Component({
  selector: 'app-ventes',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, PaginationComponent, FormsModule, NgClass, DateRangeComponent],
  templateUrl: './ventes.component.html',
})
export class VentesComponent implements OnInit {
  items: Vente[] = [];
  clients: Client[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  clientFilter = '';
  dateRange: DateRange = 'all';
  dateFrom = '';
  dateTo = '';
  page = 1;
  pageSize = 10;
  total = 0;
  stats = { ca: 0, count: 0, panier: 0, impayes: 0, nbImpayes: 0 };

  modalOpen = false;
  viewVente: Vente | null = null;
  paiementMontant = 0;
  paiementMethode = 'Espece';
  paiementSaving = false;

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  statusInfo = statusInfo;
  Math = Math;

  private searchTimer: any;

  constructor(
    private api: ApiService,
    private toast: ToastService,
    private exportSvc: ExportService,
    public router: Router,
    public auth: AuthService,
  ) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([this.load(), this.loadStats(), this.loadClients()]);
  }

  private async loadClients(): Promise<void> {
    try { this.clients = await this.api.clientsList(); } catch { /* ignore */ }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.dashboardStats();
      this.stats = {
        ca: s.caDuMois,
        count: s.ventesDuMois,
        panier: s.ventesDuMois > 0 ? s.caDuMois / s.ventesDuMois : 0,
        impayes: s.montantImpaye,
        nbImpayes: s.nombreFacturesImpayees,
      };
    } catch { /* ignore */ }
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const ranges: Record<DateRange, number> = { today: 1, '7d': 7, '30d': 30, '12m': 365, all: 0 };
      const days = ranges[this.dateRange];
      let dateDebut: string | undefined;
      let dateFin: string | undefined;
      if (this.dateFrom || this.dateTo) {
        dateDebut = this.dateFrom || undefined;
        dateFin = this.dateTo || undefined;
      } else if (days > 0) {
        dateDebut = new Date(Date.now() - days * 86400000).toISOString().split('T')[0];
      }
      const result = await this.api.ventesListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        statut: this.statusFilter === 'Partiel' ? 'EnAttente' : (this.statusFilter || undefined),
        clientId: this.clientFilter ? Number(this.clientFilter) : undefined,
        dateDebut, dateFin,
      });
      this.items = this.statusFilter === 'Partiel'
        ? result.items.filter(v => v.montantPaye > 0)
        : result.items;
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

  isOverdue(dateStr: string): boolean { return new Date(dateStr) < new Date(); }

  venteStatus(v: Vente): { label: string; cls: string } {
    if (v.statut === 'EnAttente' && v.montantPaye > 0) return { label: 'Partiel', cls: 'partial' };
    return statusInfo(v.statut);
  }

  setDateRange(r: string): void { this.dateRange = r as DateRange; this.onFilterChange(); }

  resetFilters(): void {
    this.search = ''; this.statusFilter = ''; this.clientFilter = '';
    this.dateRange = '30d'; this.dateFrom = ''; this.dateTo = ''; this.page = 1;
    this.load();
  }

  get venteDates(): string[] {
    return Array.from(new Set(this.items.map(v => {
      const d = new Date(v.dateVente);
      return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    }))).sort();
  }

  async exportExcel(): Promise<void> {
    try {
      const r = await this.api.ventesListPaged({ page: 1, pageSize: 2000 });
      this.exportSvc.exportVentes(r.items);
    } catch { this.toast.notify('Erreur export', 'error'); }
  }

  openDetail(v: Vente): void {
    this.viewVente = v; this.paiementMontant = 0; this.paiementMethode = 'Espece'; this.modalOpen = true;
  }

  async addPaiement(): Promise<void> {
    if (!this.viewVente || this.paiementMontant <= 0) return;
    this.paiementSaving = true;
    try {
      await this.api.venteAddPaiement(this.viewVente.id, { montant: this.paiementMontant, methode: this.paiementMethode });
      this.paiementMontant = 0;
      this.toast.notify('Paiement enregistré', 'success');
      this.modalOpen = false; this.viewVente = null;
      await Promise.all([this.load(), this.loadStats()]);
    } catch { this.toast.notify('Erreur lors du paiement', 'error'); }
    finally { this.paiementSaving = false; }
  }

  async handleCancel(id: number): Promise<void> {
    if (!confirm('Annuler cette vente ?')) return;
    try { await this.api.venteCancel(id); this.toast.notify('Vente annulée', 'success'); await this.load(); }
    catch { this.toast.notify("Impossible d'annuler la vente", 'error'); }
  }

  async duplicate(v: Vente): Promise<void> {
    try {
      const full = await this.api.venteGet(v.id);
      this.router.navigate(['/ventes/nouvelle'], { state: { duplicate: full } });
    } catch { this.toast.notify('Erreur lors de la duplication', 'error'); }
  }
}
