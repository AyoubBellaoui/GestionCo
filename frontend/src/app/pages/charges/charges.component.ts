import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Charge, CategorieCharge } from '../../core/models';
import { formatNum, formatDate, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-charges',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, FormsModule],
  templateUrl: './charges.component.html',
})
export class ChargesComponent implements OnInit {
  charges: Charge[] = [];
  categories: CategorieCharge[] = [];
  loading = true;

  search = '';
  statusFilter = '';
  categorieFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;

  modalOpen = false;
  viewCharge: Charge | null = null;

  paiementMontant = 0;
  paiementMethode = 'Espece';
  paiementSaving = false;

  newCatModalOpen = false;
  newCatNom = '';
  newCatIcone = '📋';
  newCatSaving = false;

  readonly emojiOptions = ['🏠','🏢','⚡','💡','🌐','🚗','✈️','👤','🔧','💧','🛡️','📎','📣','📋','💰','💳','🧾','📦','🏥','🎓','☕','🍽️','🎯','🖨️','📱'];

  formatNum = formatNum;
  formatDate = formatDate;
  getPayStatus = getPayStatus;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [c, cats] = await Promise.all([
        this.api.chargesList().catch(() => []),
        this.api.categoriesChargeList().catch(() => []),
      ]);
      this.charges = c;
      this.categories = cats;
    } finally { this.loading = false; }
  }

  chargeStatus(c: Charge): { label: string; cls: string } {
    if (c.statut === 'Paye') return { label: 'Payée', cls: 'paid' };
    if (c.statut === 'Partiel') return { label: 'Partiel', cls: 'partial' };
    if (c.statut === 'Annule') return { label: 'Annulée', cls: 'cancelled' };
    return { label: 'En attente', cls: 'pending' };
  }

  get filtered(): Charge[] {
    return this.charges.filter(c => {
      if (this.search) {
        const s = this.search.toLowerCase();
        if (!c.reference.toLowerCase().includes(s) && !c.titre.toLowerCase().includes(s)) return false;
      }
      if (this.statusFilter && c.statut !== this.statusFilter) return false;
      if (this.categorieFilter && String(c.categorieChargeId) !== this.categorieFilter) return false;
      if (this.selectedDate) {
        const d = new Date(c.dateCharge);
        const iso = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        if (iso !== this.selectedDate) return false;
      }
      return true;
    });
  }

  get stats() {
    const now = new Date();
    const thisMonth = this.charges.filter(c => {
      const d = new Date(c.dateCharge);
      return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
    });
    const totalMois = thisMonth.reduce((s, c) => s + c.montant, 0);
    const count = thisMonth.length;
    const impayes = this.charges
      .filter(c => c.statut !== 'Paye' && c.statut !== 'Annule')
      .reduce((s, c) => s + c.reste, 0);
    const nbImpayes = this.charges
      .filter(c => c.statut !== 'Paye' && c.statut !== 'Annule' && c.reste > 0).length;
    const totalGlobal = this.charges.reduce((s, c) => s + c.montant, 0);
    return { totalMois, count, impayes, nbImpayes, totalGlobal };
  }

  get total(): number { return this.filtered.length; }
  get pageCount(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }
  get paged(): Charge[] { return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.categorieFilter = '';
    this.selectedDate = '';
    this.page = 1;
  }

  openDetail(c: Charge): void {
    this.viewCharge = c;
    this.paiementMontant = 0;
    this.paiementMethode = 'Espece';
    this.modalOpen = true;
  }

  async addPaiement(): Promise<void> {
    if (!this.viewCharge || this.paiementMontant <= 0) return;
    this.paiementSaving = true;
    try {
      const updated = await this.api.chargeAddPaiement(this.viewCharge.id, {
        montant: this.paiementMontant,
        methode: this.paiementMethode,
      });
      this.charges = this.charges.map(c => c.id === updated.id ? updated : c);
      this.viewCharge = updated;
      this.paiementMontant = 0;
      this.toast.notify('Paiement enregistré', 'success');
    } catch {
      this.toast.notify('Erreur lors du paiement', 'error');
    } finally {
      this.paiementSaving = false;
    }
  }

  openNewCatModal(): void {
    this.newCatNom = '';
    this.newCatIcone = '📋';
    this.newCatModalOpen = true;
  }

  async saveNewCat(): Promise<void> {
    if (!this.newCatNom.trim()) { this.toast.notify('Le nom est requis', 'warning'); return; }
    this.newCatSaving = true;
    try {
      const created = await this.api.categorieChargeCreate({ nom: this.newCatNom.trim(), icone: this.newCatIcone });
      this.categories = [...this.categories, created].sort((a, b) => a.nom.localeCompare(b.nom));
      this.newCatModalOpen = false;
      this.toast.notify(`Catégorie « ${created.nom} » créée`, 'success');
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.newCatSaving = false; }
  }

  async deleteCat(id: number, nom: string): Promise<void> {
    if (!confirm(`Supprimer la catégorie « ${nom} » ?`)) return;
    try {
      await this.api.categorieChargeDelete(id);
      this.categories = this.categories.filter(c => c.id !== id);
      this.toast.notify('Catégorie supprimée', 'success');
    } catch (e: any) {
      this.toast.notify(e?.error?.message || 'Impossible de supprimer cette catégorie', 'error');
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
