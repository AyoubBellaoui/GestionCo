import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { Facture, VenteSansFacture } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-factures',
  standalone: true,
  imports: [TopbarComponent, ModalComponent, FormsModule, NgClass],
  templateUrl: './factures.component.html',
})
export class FacturesComponent implements OnInit {
  factures: Facture[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  showDetail: Facture | null = null;
  showInfoModal = false;
  showCreateModal = false;
  ventesSansFacture: VenteSansFacture[] = [];
  selectedVenteId: number | null = null;
  dateEmission = '';
  dateEcheance = '';
  creating = false;
  searchVente = '';

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.factures = await this.api.facturesList().catch(() => []); }
    finally { this.loading = false; }
  }

  factureStatus(f: Facture): { label: string; cls: string } {
    if (f.statut === 'PartiellementPayee')
      return { label: 'Partiel', cls: 'partial' };
    if ((f.statut === 'EnAttente' || f.statut === 'EnRetard') && f.montantPaye > 0)
      return { label: 'Partiel', cls: 'partial' };
    if (f.statut === 'Payee' || f.statut === 'Paye')
      return { label: 'Payée', cls: 'good' };
    if (f.statut === 'EnRetard')
      return { label: 'En retard', cls: 'low' };
    if (f.statut === 'EnAttente')
      return { label: 'En attente', cls: 'pending' };
    return { label: f.statutLibelle, cls: 'pending' };
  }

  get filtered(): Facture[] {
    return this.factures.filter(f => {
      if (this.search && !f.numeroFacture.toLowerCase().includes(this.search.toLowerCase()) &&
        !f.nomClient.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.statusFilter === 'Partiel') {
        if (!(f.statut === 'PartiellementPayee' || ((f.statut === 'EnAttente' || f.statut === 'EnRetard') && f.montantPaye > 0))) return false;
      } else if (this.statusFilter && f.statut !== this.statusFilter) {
        return false;
      }
      return true;
    });
  }

  get stats() {
    return {
      total: this.factures.reduce((s, f) => s + f.montantTotal, 0),
      paye: this.factures.reduce((s, f) => s + f.montantPaye, 0),
      enRetard: this.factures.filter(f => f.estEnRetard).length,
      count: this.factures.length,
    };
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
    echeance.setDate(today.getDate() + 30);
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

  statutCls(s: string): string {
    if (s === 'Paye' || s === 'Payee') return 'good';
    if (s === 'PartielPaye') return 'medium';
    if (s === 'EnRetard') return 'low';
    return 'pending';
  }

  resetFilters(): void { this.search = ''; this.statusFilter = ''; }
}
