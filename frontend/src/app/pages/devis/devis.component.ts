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
  clients: Client[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  clientFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;

  formatNum = formatNum;
  formatDate = formatDate;
  getAvatarClass = getAvatarClass;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [d, c] = await Promise.all([
        this.api.devisList().catch(() => []),
        this.api.clientsList().catch(() => []),
      ]);
      this.devisList = d;
      this.clients = c;
    } finally { this.loading = false; }
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

  get filtered(): Devis[] {
    return this.devisList.filter(d => {
      if (this.search && !(d.reference.toLowerCase().includes(this.search.toLowerCase()) || d.nomClient.toLowerCase().includes(this.search.toLowerCase()))) return false;
      if (this.statusFilter === 'Expire') {
        if (!d.estExpire && d.statut !== 'Expire') return false;
      } else if (this.statusFilter && d.statut !== this.statusFilter) return false;
      if (this.clientFilter && String(d.clientId) !== this.clientFilter) return false;
      if (this.selectedDate) {
        const dd = new Date(d.dateDevis);
        const dStr = `${dd.getFullYear()}-${String(dd.getMonth()+1).padStart(2,'0')}-${String(dd.getDate()).padStart(2,'0')}`;
        if (dStr !== this.selectedDate) return false;
      }
      return true;
    });
  }

  get stats() {
    const now = new Date();
    const mo = this.devisList.filter(d => {
      const dd = new Date(d.dateDevis);
      return dd.getMonth() === now.getMonth() && dd.getFullYear() === now.getFullYear();
    });
    const total = mo.length;
    const acceptes = mo.filter(d => d.statut === 'Accepte').length;
    const convertis = this.devisList.filter(d => d.statut === 'Converti').length;
    const montantPotentiel = this.devisList.filter(d => d.statut === 'Envoye' || d.statut === 'Brouillon').reduce((s, d) => s + d.montantTotal, 0);
    const tauxAcceptation = total > 0 ? Math.round((acceptes / total) * 100) : 0;
    return { total, acceptes, convertis, montantPotentiel, tauxAcceptation };
  }

  get totalFiltered(): number { return this.filtered.length; }
  get paged(): Devis[] { return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }

  resetFilters(): void { this.search = ''; this.statusFilter = ''; this.clientFilter = ''; this.selectedDate = ''; this.page = 1; }

  async marquerEnvoye(d: Devis): Promise<void> {
    try {
      const updated = await this.api.devisUpdateStatut(d.id, 'Envoye');
      this.devisList = this.devisList.map(x => x.id === updated.id ? updated : x);
      this.toast.notify('Devis marqué comme envoyé', 'success');
    } catch { this.toast.notify('Erreur lors de la mise à jour', 'error'); }
  }

  async accepter(d: Devis): Promise<void> {
    if (!confirm(`Marquer le devis ${d.reference} comme accepté ?`)) return;
    try {
      const updated = await this.api.devisUpdateStatut(d.id, 'Accepte');
      this.devisList = this.devisList.map(x => x.id === updated.id ? updated : x);
      this.toast.notify('Devis accepté', 'success');
    } catch { this.toast.notify('Erreur lors de la mise à jour', 'error'); }
  }

  async refuser(d: Devis): Promise<void> {
    if (!confirm(`Marquer le devis ${d.reference} comme refusé ?`)) return;
    try {
      const updated = await this.api.devisUpdateStatut(d.id, 'Refuse');
      this.devisList = this.devisList.map(x => x.id === updated.id ? updated : x);
      this.toast.notify('Devis refusé', 'info');
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
      this.devisList = this.devisList.filter(x => x.id !== d.id);
      this.toast.notify('Devis supprimé', 'success');
    } catch { this.toast.notify('Erreur lors de la suppression', 'error'); }
  }

  shareWhatsApp(d: Devis): void {
    const entreprise = this.settings.settings.entreprise.raisonSociale || 'GestionCo';
    const client = this.clients.find(c => c.id === d.clientId)?.nomClient || '';
    const msg = `Bonjour${client ? ' ' + client : ''},\n\nVeuillez trouver ci-joint notre devis *${d.reference}*.\n\nMontant TTC : *${d.montantTotal.toLocaleString('fr-FR')} MAD*${d.dateValidite ? '\nValide jusqu\'au : ' + new Date(d.dateValidite).toLocaleDateString('fr-FR') : ''}\n\nCordialement,\n${entreprise}`;
    window.open(`https://wa.me/?text=${encodeURIComponent(msg)}`, '_blank');
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
