import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';
import { Achat, Fournisseur } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-achats',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, FormsModule],
  templateUrl: './achats.component.html',
})
export class AchatsComponent implements OnInit {
  achats: Achat[] = [];
  fournisseurs: Fournisseur[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  fournisseurFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;
  modalOpen = false;
  viewAchat: Achat | null = null;

  // Paiement rapide depuis le modal
  paiementMontant = 0;
  paiementMethode = 'Espece';
  paiementSaving = false;

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, private exportSvc: ExportService, public router: Router) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  exportExcel(): void { this.exportSvc.exportAchats(this.achats); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [a, f] = await Promise.all([
        this.api.achatsList().catch(() => []),
        this.api.fournisseursList().catch(() => []),
      ]);
      this.achats = a;
      this.fournisseurs = f;
    } finally { this.loading = false; }
  }

  achatStatus(a: Achat): { label: string; cls: string } {
    if (a.statut === 'Paye') return { label: 'Payé', cls: 'paid' };
    if (a.statut === 'Partiel') return { label: 'Partiel', cls: 'partial' };
    if (a.statut === 'Annule') return { label: 'Annulé', cls: 'cancelled' };
    return { label: 'Crédit', cls: 'pending' };
  }

  get filtered(): Achat[] {
    return this.achats.filter(a => {
      if (this.search && !a.reference.toLowerCase().includes(this.search.toLowerCase()) &&
        !(a.nomFournisseur || '').toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.statusFilter && a.statut !== this.statusFilter) return false;
      if (this.fournisseurFilter && String(a.fournisseurId) !== this.fournisseurFilter) return false;
      if (this.selectedDate) {
        const d = new Date(a.dateAchat);
        const iso = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        if (iso !== this.selectedDate) return false;
      }
      return true;
    });
  }

  get stats() {
    const now = new Date();
    const thisMonth = this.achats.filter(a => {
      const d = new Date(a.dateAchat);
      return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
    });
    const depenses = thisMonth.reduce((s, a) => s + a.montantTotal, 0);
    const count = thisMonth.length;
    const fournisseurs = new Set(this.achats.map(a => a.fournisseurId)).size;
    const impayes = this.achats.filter(a => a.statut !== 'Paye' && a.statut !== 'Annule').reduce((s, a) => s + a.reste, 0);
    const nbImpayes = this.achats.filter(a => a.statut !== 'Paye' && a.statut !== 'Annule' && a.reste > 0).length;
    return { depenses, count, fournisseurs, impayes, nbImpayes };
  }

  get total(): number { return this.filtered.length; }
  get pageCount(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }
  get paged(): Achat[] { return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.fournisseurFilter = '';
    this.selectedDate = '';
    this.page = 1;
  }

  openDetail(a: Achat): void {
    this.viewAchat = a;
    this.paiementMontant = 0;
    this.paiementMethode = 'Espece';
    this.modalOpen = true;
  }

  async addPaiement(): Promise<void> {
    if (!this.viewAchat || this.paiementMontant <= 0) return;
    this.paiementSaving = true;
    try {
      const updated = await this.api.achatAddPaiement(this.viewAchat.id, {
        montant: this.paiementMontant,
        methode: this.paiementMethode,
      });
      this.achats = this.achats.map(a => a.id === updated.id ? updated : a);
      this.paiementMontant = 0;
      this.toast.notify('Paiement enregistré', 'success');
      this.modalOpen = false;
      this.viewAchat = null;
    } catch {
      this.toast.notify('Erreur lors du paiement', 'error');
    } finally {
      this.paiementSaving = false;
    }
  }

  buildPageList(): (number | '…')[] {
    const total = this.pageCount;
    if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
    const pages: (number | '…')[] = [1];
    if (this.page > 3) pages.push('…');
    for (let i = Math.max(2, this.page - 1); i <= Math.min(total - 1, this.page + 1); i++) pages.push(i);
    if (this.page < total - 2) pages.push('…');
    pages.push(total);
    return pages;
  }

  isPageNum(p: number | '…'): p is number { return p !== '…'; }
}
