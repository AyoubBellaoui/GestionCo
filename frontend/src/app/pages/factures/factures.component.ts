import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { Facture, VenteSansFacture } from '../../core/models';
import * as XLSX from 'xlsx';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

type Tab = 'all' | 'payee' | 'partiel' | 'enAttente' | 'enRetard' | 'annulee';
type SortField = '' | 'numero' | 'client' | 'dateEmission' | 'montant';

@Component({
  selector: 'app-factures',
  standalone: true,
  imports: [TopbarComponent, ModalComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './factures.component.html',
})
export class FacturesComponent implements OnInit {
  factures: Facture[] = [];
  totalCount = 0;
  loading = true;
  page = 1;
  pageSize = 15;
  search = '';
  activeTab: Tab = 'all';
  selectedDate = '';
  clientFilter = '';
  sortField: SortField = '';
  sortDir: 'asc' | 'desc' = 'desc';
  selectedIds = new Set<number>();
  statsData = { totalMois: 0, totalMoisTrend: null as number | null, totalPaye: 0, payeePct: 0, enAttenteCount: 0, enAttenteMontant: 0, enRetardCount: 0, enRetardMontant: 0, tabCounts: { all: 0, payee: 0, partiel: 0, enAttente: 0, enRetard: 0, annulee: 0 } };
  uniqueClients: string[] = [];
  private searchTimer: any;

  showDetail: Facture | null = null;
  showInfoModal = false;
  showCreateModal = false;
  ventesSansFacture: VenteSansFacture[] = [];
  selectedVenteId: number | null = null;
  dateEmission = '';
  dateEcheance = '';
  creating = false;
  searchVente = '';

  emailModalOpen = false;
  emailTarget: Facture | null = null;
  emailTo = '';
  emailMessage = '';
  emailSending = false;

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats(), this.loadClients()]); }

  private async loadClients(): Promise<void> {
    try {
      const clients = await this.api.clientsList();
      this.uniqueClients = clients.map(c => c.nomClient).sort();
    } catch (err) { console.error('loadClients factures error:', err); }
  }

  private tabToParams(): { statut?: string; estEnRetard?: boolean } {
    switch (this.activeTab) {
      case 'payee':     return { statut: 'Payee' };
      case 'partiel':   return { statut: 'PartiellementPayee' };
      case 'enAttente': return { statut: 'EnAttente' };
      case 'enRetard':  return { estEnRetard: true };
      case 'annulee':   return { statut: 'Annulee' };
      default:          return {};
    }
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const tabParams = this.tabToParams();
      const result = await this.api.facturesListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        dateFilter: this.selectedDate || undefined,
        ...tabParams,
      });
      this.factures = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.facturesStats();
      this.statsData = {
        totalMois: s.totalMois, totalMoisTrend: null, totalPaye: s.totalPaye,
        payeePct: s.payeePct,
        enAttenteCount: s.enAttenteCount, enAttenteMontant: s.enAttenteMontant,
        enRetardCount: s.enRetardCount, enRetardMontant: s.enRetardMontant,
        tabCounts: s.tabCounts,
      };
    } catch (err) { console.error('loadStats factures error:', err); }
  }

  factureStatus(f: Facture): { label: string; cls: string } {
    if (f.statut === 'Annulee') return { label: 'Annulée', cls: 'cancelled' };
    if (f.statut === 'PartiellementPayee') return { label: 'Partiel', cls: 'partial' };
    if ((f.statut === 'EnAttente' || f.statut === 'EnRetard') && f.montantPaye > 0)
      return { label: 'Partiel', cls: 'partial' };
    if (f.statut === 'Payee' || f.statut === 'Paye') return { label: 'Payée', cls: 'good' };
    if (f.statut === 'EnRetard') return { label: 'En retard', cls: 'low' };
    if (f.statut === 'EnAttente') return { label: 'En attente', cls: 'pending' };
    return { label: f.statutLibelle, cls: 'pending' };
  }

  private isPartiel(f: Facture): boolean {
    return f.statut === 'PartiellementPayee' ||
      ((f.statut === 'EnAttente' || f.statut === 'EnRetard') && f.montantPaye > 0);
  }

  get filtered(): Facture[] { return this.factures; }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }

  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  toggleSort(field: SortField): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'desc';
    }
    this.page = 1;
    this.load();
  }

  sortIcon(field: SortField): string {
    if (this.sortField !== field) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  get stats() { return this.statsData; }

  get paged(): Facture[] { return this.factures; }

  get filteredTotal(): number {
    return this.factures.reduce((s, f) => s + f.montantTotal, 0);
  }

  get tabCount() { return this.statsData.tabCounts; }

  get allSelected(): boolean {
    return this.filtered.length > 0 && this.filtered.every(f => this.selectedIds.has(f.id));
  }

  toggleAll(): void {
    if (this.allSelected) {
      this.filtered.forEach(f => this.selectedIds.delete(f.id));
    } else {
      this.filtered.forEach(f => this.selectedIds.add(f.id));
    }
    this.selectedIds = new Set(this.selectedIds);
  }

  toggleSelect(id: number): void {
    if (this.selectedIds.has(id)) this.selectedIds.delete(id);
    else this.selectedIds.add(id);
    this.selectedIds = new Set(this.selectedIds);
  }

  exportExcel(): void {
    const rows = this.filtered.map(f => ({
      'N° Facture':     f.numeroFacture,
      'Client':         f.nomClient,
      'ICE':            f.clientICE || '',
      'Date Émission':  f.dateEmission.split('T')[0],
      'Date Échéance':  f.dateEcheance.split('T')[0],
      'Montant Total':  f.montantTotal,
      'Montant Payé':   f.montantPaye,
      'Reste Dû':       f.montantTotal - f.montantPaye,
      'Statut':         f.statutLibelle,
    }));
    const ws = XLSX.utils.json_to_sheet(rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Factures');
    XLSX.writeFile(wb, `factures-${new Date().toISOString().slice(0, 10)}.xlsx`);
    this.toast.notify('Export Excel téléchargé', 'success');
  }

  async downloadSelected(): Promise<void> {
    const ids = [...this.selectedIds];
    if (ids.length === 0) { this.toast.notify('Sélectionnez des factures d\'abord', 'warning'); return; }
    this.toast.notify(`Téléchargement de ${ids.length} facture(s)…`, 'info');
    for (const id of ids) {
      const f = this.factures.find(x => x.id === id);
      if (f) await this.downloadPdf(f.id, f.numeroFacture);
    }
  }

  get ventesFiltrees(): VenteSansFacture[] {
    if (!this.searchVente) return this.ventesSansFacture;
    const s = this.searchVente.toLowerCase();
    return this.ventesSansFacture.filter(v =>
      v.reference.toLowerCase().includes(s) || v.nomClient.toLowerCase().includes(s)
    );
  }

  async openCreateModal(): Promise<void> {
    this.showCreateModal = true;
    this.selectedVenteId = null;
    this.searchVente = '';
    const today = new Date();
    const echeance = new Date();
    echeance.setDate(today.getDate() + this.settings.settings.facturation.delaiPaiement);
    this.dateEmission = today.toISOString().split('T')[0];
    this.dateEcheance = echeance.toISOString().split('T')[0];
    try { this.ventesSansFacture = await this.api.factureGetVentesSansFacture(); }
    catch { this.toast.notify('Erreur lors du chargement des ventes', 'error'); }
  }

  async createFacture(): Promise<void> {
    if (!this.selectedVenteId) { this.toast.notify('Sélectionnez une vente', 'warning'); return; }
    this.creating = true;
    try {
      await this.api.factureCreate({
        venteId: this.selectedVenteId,
        dateEmission: this.dateEmission || undefined,
        dateEcheance: this.dateEcheance || undefined,
      });
      this.toast.notify('Facture créée avec succès', 'success');
      this.showCreateModal = false;
      this.load();
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.creating = false; }
  }

  async openInvoice(id: number): Promise<void> {
    try {
      const blob = await this.api.factureDownloadPdf(id, this.settings.settings.entreprise as any);
      const url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
      window.open(url, '_blank');
      setTimeout(() => URL.revokeObjectURL(url), 60000);
    } catch { this.toast.notify('Erreur lors du chargement', 'error'); }
  }

  async downloadPdf(id: number, ref: string): Promise<void> {
    try {
      const blob = await this.api.factureDownloadPdf(id, this.settings.settings.entreprise as any);
      const url = URL.createObjectURL(new Blob([blob], { type: 'application/pdf' }));
      const a = document.createElement('a');
      a.href = url; a.download = `Facture-${ref}.pdf`; a.click();
      URL.revokeObjectURL(url);
      this.toast.notify('Facture PDF téléchargée', 'success');
    } catch { this.toast.notify('Erreur lors du téléchargement', 'error'); }
  }

  openEmailModal(f: Facture): void {
    this.emailTarget = f;
    this.emailTo = '';
    this.emailMessage = '';
    this.emailModalOpen = true;
  }

  async sendEmail(): Promise<void> {
    if (!this.emailTarget || !this.emailTo.trim()) {
      this.toast.notify('Adresse email requise', 'warning'); return;
    }
    this.emailSending = true;
    try {
      await this.api.factureEmail(this.emailTarget.id, this.emailTo.trim(), this.emailMessage.trim() || undefined, this.settings.settings.entreprise as any);
      this.emailModalOpen = false;
      this.toast.notify(`Facture envoyée à ${this.emailTo}`, 'success');
      await this.load();
    } catch (e: any) {
      this.toast.notify(e?.error?.message || e?.error?.detail || 'Erreur lors de l\'envoi', 'error');
    } finally { this.emailSending = false; }
  }

  statutCls(s: string): string {
    if (s === 'Paye' || s === 'Payee') return 'good';
    if (s === 'PartielPaye') return 'medium';
    if (s === 'EnRetard') return 'low';
    return 'pending';
  }

  clearSelection(): void { this.selectedIds = new Set(); }

  onSortChange(val: string): void {
    const [field, dir] = val.split(':');
    this.sortField = (field as SortField) || '';
    this.sortDir = (dir as 'asc' | 'desc') || 'desc';
    this.page = 1;
    this.load();
  }

  resetFilters(): void {
    this.search = '';
    this.activeTab = 'all';
    this.selectedDate = '';
    this.clientFilter = '';
    this.sortField = '';
    this.selectedIds = new Set();
    this.page = 1;
    this.load();
  }
}
