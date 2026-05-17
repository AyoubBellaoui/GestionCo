import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule, NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Devis, Client } from '../../core/models';
import { formatNum, formatDate, getAvatarClass } from '../../core/utils/format';
import { SettingsService } from '../../core/services/settings.service';

@Component({
  selector: 'app-devis',
  standalone: true,
  imports: [CommonModule, TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './devis.component.html',
})
export class DevisComponent implements OnInit {
  devisList: Devis[] = [];
  totalCount = 0;
  clients: Client[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  clientFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;
  statsData = { total: 0, acceptes: 0, convertis: 0, montantPotentiel: 0, tauxAcceptation: 0 };
  private searchTimer: any;

  formatNum = formatNum;
  formatDate = formatDate;
  getAvatarClass = getAvatarClass;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats(), this.loadClients()]); }

  private async loadClients(): Promise<void> {
    try { this.clients = await this.api.clientsList().catch(() => []); } catch {}
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.devisListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        statut: this.statusFilter || undefined,
        clientId: this.clientFilter ? Number(this.clientFilter) : undefined,
        dateDebut: this.selectedDate || undefined,
        dateFin: this.selectedDate || undefined,
      });
      this.devisList = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.devisStats();
      this.statsData = { total: s.total, acceptes: s.acceptes, convertis: s.convertis, montantPotentiel: s.montantPotentiel, tauxAcceptation: s.tauxAcceptation };
    } catch (err) { console.error('loadStats devis error:', err); }
  }

  statutInfo(d: Devis): { label: string; cls: string } {
    if (d.estExpire) return { label: 'Expiré', cls: 'bad' };
    switch (d.statut) {
      case 'Brouillon': return { label: 'Brouillon', cls: 'neutral' };
      case 'Envoye':    return { label: 'Envoyé', cls: 'medium' };
      case 'Accepte':   return { label: 'Accepté', cls: 'good' };
      case 'Refuse':    return { label: 'Refusé', cls: 'bad' };
      case 'Expire':    return { label: 'Expiré', cls: 'bad' };
      case 'Converti':  return { label: 'Converti ✓', cls: 'partial' };
      default:          return { label: d.statut, cls: 'neutral' };
    }
  }

  get filtered(): Devis[] { return this.devisList; }
  get stats() { return this.statsData; }
  get totalFiltered(): number { return this.totalCount; }
  get paged(): Devis[] { return this.devisList; }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  resetFilters(): void { this.search = ''; this.statusFilter = ''; this.clientFilter = ''; this.selectedDate = ''; this.page = 1; this.load(); }

  async marquerEnvoye(d: Devis): Promise<void> {
    try {
      await this.api.devisUpdateStatut(d.id, 'Envoye');
      this.toast.notify('Devis marqué comme envoyé', 'success');
      this.load();
    } catch { this.toast.notify('Erreur lors de la mise à jour', 'error'); }
  }

  async accepter(d: Devis): Promise<void> {
    if (!confirm(`Marquer le devis ${d.reference} comme accepté ?`)) return;
    try {
      await this.api.devisUpdateStatut(d.id, 'Accepte');
      this.toast.notify('Devis accepté', 'success');
      this.load();
    } catch { this.toast.notify('Erreur lors de la mise à jour', 'error'); }
  }

  async refuser(d: Devis): Promise<void> {
    if (!confirm(`Marquer le devis ${d.reference} comme refusé ?`)) return;
    try {
      await this.api.devisUpdateStatut(d.id, 'Refuse');
      this.toast.notify('Devis refusé', 'info');
      this.load();
    } catch { this.toast.notify('Erreur lors de la mise à jour', 'error'); }
  }

  async convertir(d: Devis): Promise<void> {
    if (!confirm(`Convertir le devis ${d.reference} en vente ? Cela réduira le stock.`)) return;
    try {
      const result = await this.api.devisConvertir(d.id);
      this.toast.notify(`Vente ${result.venteReference} créée avec succès !`, 'success');
      await this.load();
      this.router.navigate(['/ventes']);
    } catch (e: any) {
      this.toast.notify(e?.error?.message || 'Erreur lors de la conversion', 'error');
    }
  }

  async supprimer(d: Devis): Promise<void> {
    if (!confirm(`Supprimer le devis ${d.reference} ?`)) return;
    try {
      await this.api.devisDelete(d.id);
      this.toast.notify('Devis supprimé', 'success');
      this.load();
    } catch { this.toast.notify('Erreur lors de la suppression', 'error'); }
  }

  async shareWhatsApp(d: Devis): Promise<void> {
    const entreprise = this.settings.settings.entreprise.raisonSociale || 'GestionCo';
    const client = this.clients.find(c => c.id === d.clientId)?.nomClient || '';
    try {
      const { url } = await this.api.devisShareLink(d.id);
      const msg = `Bonjour${client ? ' ' + client : ''},\n\nVeuillez trouver notre devis *${d.reference}* en cliquant sur le lien ci-dessous :\n\n📄 ${url}\n\nMontant TTC : *${d.montantTotal.toLocaleString('fr-FR')} MAD*${d.dateValidite ? '\nValide jusqu\'au : ' + new Date(d.dateValidite).toLocaleDateString('fr-FR') : ''}\n\nCordialement,\n${entreprise}`;
      window.open(`https://wa.me/?text=${encodeURIComponent(msg)}`, '_blank');
    } catch {
      this.toast.notify('Erreur lors de la génération du lien de partage', 'error');
    }
  }

  async printPdf(d: Devis): Promise<void> {
    try {
      const blob = await this.api.devisPdf(d.id, this.settings.settings.entreprise as any);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = `Devis-${d.reference}.pdf`; a.click();
      URL.revokeObjectURL(url);
    } catch { this.toast.notify('Erreur lors de la génération du PDF', 'error'); }
  }

  emailModalOpen = false;
  emailTarget: Devis | null = null;
  emailTo = '';
  emailMessage = '';
  emailSending = false;

  openEmailModal(d: Devis): void {
    this.emailTarget = d;
    this.emailTo = this.clients.find(c => c.id === d.clientId)?.email || '';
    this.emailMessage = '';
    this.emailModalOpen = true;
  }

  async sendEmail(): Promise<void> {
    if (!this.emailTarget || !this.emailTo.trim()) {
      this.toast.notify('Adresse email requise', 'warning'); return;
    }
    this.emailSending = true;
    try {
      await this.api.devisEmail(this.emailTarget.id, this.emailTo.trim(), this.emailMessage.trim() || undefined, this.settings.settings.entreprise as any);
      await this.api.devisUpdateStatut(this.emailTarget.id, 'Envoye');
      await this.load();
      this.emailModalOpen = false;
      this.toast.notify(`Devis envoyé à ${this.emailTo}`, 'success');
    } catch (e: any) {
      const msg = e?.error?.message || e?.error?.detail || e?.message || 'Erreur lors de l\'envoi email';
      this.toast.notify(msg, 'error');
    } finally { this.emailSending = false; }
  }

}
