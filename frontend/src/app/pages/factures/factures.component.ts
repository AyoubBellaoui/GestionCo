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

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.factures = await this.api.facturesList().catch(() => []); }
    finally { this.loading = false; }
  }

  get uniqueClients(): string[] {
    return [...new Set(this.factures.map(f => f.nomClient))].sort();
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

  private filterByTab(f: Facture): boolean {
    switch (this.activeTab) {
      case 'payee': return (f.statut === 'Payee' || f.statut === 'Paye') && !this.isPartiel(f);
      case 'partiel': return this.isPartiel(f);
      case 'enAttente': return f.statut === 'EnAttente' && !f.estEnRetard && !this.isPartiel(f);
      case 'enRetard': return (f.estEnRetard || f.statut === 'EnRetard') && !this.isPartiel(f);
      case 'annulee': return f.statut === 'Annulee';
      default: return true;
    }
  }

  private filterByDate(f: Facture): boolean {
    if (!this.selectedDate) return true;
    const d = new Date(f.dateEmission);
    const dStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    return dStr === this.selectedDate;
  }

  get filtered(): Facture[] {
    let result = this.factures.filter(f => {
      if (!this.filterByTab(f)) return false;
      if (!this.filterByDate(f)) return false;
      if (this.search) {
        const s = this.search.toLowerCase();
        if (!f.numeroFacture.toLowerCase().includes(s) && !f.nomClient.toLowerCase().includes(s)) return false;
      }
      if (this.clientFilter && f.nomClient !== this.clientFilter) return false;
      return true;
    });

    if (this.sortField) {
      result = [...result].sort((a, b) => {
        let va: any, vb: any;
        switch (this.sortField) {
          case 'numero': va = a.numeroFacture; vb = b.numeroFacture; break;
          case 'client': va = a.nomClient; vb = b.nomClient; break;
          case 'dateEmission': va = new Date(a.dateEmission).getTime(); vb = new Date(b.dateEmission).getTime(); break;
          case 'montant': va = a.montantTotal; vb = b.montantTotal; break;
          default: return 0;
        }
        const cmp = va < vb ? -1 : va > vb ? 1 : 0;
        return this.sortDir === 'asc' ? cmp : -cmp;
      });
    }

    return result;
  }

  toggleSort(field: SortField): void {
    if (this.sortField === field) {
      this.sortDir = this.sortDir === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDir = 'desc';
    }
  }

  sortIcon(field: SortField): string {
    if (this.sortField !== field) return '↕';
    return this.sortDir === 'asc' ? '↑' : '↓';
  }

  get stats() {
    const now = new Date();
    const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
    const startPrevMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    const endPrevMonth = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59);

    const factesMois = this.factures.filter(f => new Date(f.dateEmission) >= startOfMonth);
    const factesPrevMois = this.factures.filter(f => {
      const d = new Date(f.dateEmission);
      return d >= startPrevMonth && d <= endPrevMonth;
    });

    const totalMois = factesMois.reduce((s, f) => s + f.montantTotal, 0);
    const totalPrevMois = factesPrevMois.reduce((s, f) => s + f.montantTotal, 0);
    const totalMoisTrend = totalPrevMois > 0 ? Math.round(((totalMois - totalPrevMois) / totalPrevMois) * 100) : null;

    const totalPaye = this.factures.reduce((s, f) => s + f.montantPaye, 0);
    const totalFacture = this.factures.reduce((s, f) => s + f.montantTotal, 0);
    const payeePct = totalFacture > 0 ? Math.round((totalPaye / totalFacture) * 100) : 0;

    const enAttente = this.factures.filter(f => f.statut === 'EnAttente' && !f.estEnRetard);
    const enAttenteMontant = enAttente.reduce((s, f) => s + (f.montantTotal - f.montantPaye), 0);

    const enRetard = this.factures.filter(f => f.estEnRetard || f.statut === 'EnRetard');
    const enRetardMontant = enRetard.reduce((s, f) => s + (f.montantTotal - f.montantPaye), 0);

    return {
      totalMois, totalMoisTrend, totalPaye, payeePct,
      enAttenteCount: enAttente.length,
      enAttenteMontant,
      enRetardCount: enRetard.length,
      enRetardMontant,
    };
  }

  get paged(): Facture[] {
    return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize);
  }

  get filteredTotal(): number {
    return this.filtered.reduce((s, f) => s + f.montantTotal, 0);
  }

  get tabCount() {
    return {
      all: this.factures.length,
      payee: this.factures.filter(f => (f.statut === 'Payee' || f.statut === 'Paye') && !this.isPartiel(f)).length,
      partiel: this.factures.filter(f => this.isPartiel(f)).length,
      enAttente: this.factures.filter(f => f.statut === 'EnAttente' && !f.estEnRetard && !this.isPartiel(f)).length,
      enRetard: this.factures.filter(f => (f.estEnRetard || f.statut === 'EnRetard') && !this.isPartiel(f)).length,
      annulee: this.factures.filter(f => f.statut === 'Annulee').length,
    };
  }

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

  exportCsv(): void {
    const rows = [['N° Facture', 'Client', 'ICE', 'Date Emission', 'Date Echeance', 'Montant Total', 'Montant Payé', 'Statut']];
    this.filtered.forEach(f => {
      rows.push([
        f.numeroFacture, f.nomClient, f.clientICE || '',
        f.dateEmission.split('T')[0], f.dateEcheance.split('T')[0],
        String(f.montantTotal), String(f.montantPaye), f.statutLibelle,
      ]);
    });
    const csv = rows.map(r => r.map(c => `"${c}"`).join(',')).join('\n');
    const blob = new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a'); a.href = url; a.download = 'factures.csv'; a.click();
    URL.revokeObjectURL(url);
    this.toast.notify('Export CSV téléchargé', 'success');
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
  }

  resetFilters(): void {
    this.search = '';
    this.activeTab = 'all';
    this.selectedDate = '';
    this.clientFilter = '';
    this.sortField = '';
    this.selectedIds = new Set();
  }
}
