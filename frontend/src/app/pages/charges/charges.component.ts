import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Charge, CategorieCharge } from '../../core/models';
import { formatNum, formatDate, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-charges',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, PaginationComponent, FormsModule],
  templateUrl: './charges.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChargesComponent implements OnInit {
  @Input() hideTopbar = false;

  charges: Charge[] = [];
  totalCount = 0;
  categories: CategorieCharge[] = [];
  loading = true;

  search = '';
  statusFilter = '';
  categorieFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;
  statsData = { totalMois: 0, count: 0, impayes: 0, nbImpayes: 0, totalGlobal: 0 };
  private searchTimer: any;

  modalOpen = false;
  viewCharge: Charge | null = null;

  paiementMontant = 0;
  paiementMethode = 'Espece';
  paiementSaving = false;

  // Edit modal
  editModalOpen = false;
  editCharge: Charge | null = null;
  editTitre = '';
  editDescription = '';
  editMontant = 0;
  editCategorieId: number | '' = '';
  editDateCharge = '';
  editFournisseurId: number | '' = '';
  editJustificatif = '';
  editEstRecurrente = false;
  editPeriodicite = 'Mensuelle';
  editSaving = false;
  fournisseurs: any[] = [];

  newCatModalOpen = false;
  newCatNom = '';
  newCatIcone = '📋';
  newCatSaving = false;
  generatingRecurrentes = false;

  readonly emojiOptions = ['🏠','🏢','⚡','💡','🌐','🚗','✈️','👤','🔧','💧','🛡️','📎','📣','📋','💰','💳','🧾','📦','🏥','🎓','☕','🍽️','🎯','🖨️','📱'];

  formatNum = formatNum;
  formatDate = formatDate;
  getPayStatus = getPayStatus;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private cdr: ChangeDetectorRef) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats(), this.loadMeta()]); }

  private async loadMeta(): Promise<void> {
    try {
      const [cats, fournisseurs] = await Promise.all([
        this.api.categoriesChargeList().catch(() => []),
        this.api.fournisseursList().catch(() => []),
      ]);
      this.categories = cats;
      this.fournisseurs = fournisseurs;
      this.cdr.markForCheck();
    } catch {}
  }

  async load(): Promise<void> {
    this.loading = true;
    this.cdr.markForCheck();
    try {
      const result = await this.api.chargesListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        statut: this.statusFilter || undefined,
        categorieId: this.categorieFilter ? Number(this.categorieFilter) : undefined,
        dateDebut: this.selectedDate || undefined,
        dateFin: this.selectedDate || undefined,
      });
      this.charges = result.items;
      this.totalCount = result.totalCount;
    } finally {
      this.loading = false;
      this.cdr.markForCheck();
    }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.chargesStats();
      this.statsData = { totalMois: s.totalMois, count: s.count, impayes: s.impayes, nbImpayes: s.nbImpayes, totalGlobal: s.totalGlobal };
      this.cdr.markForCheck();
    } catch (err) { console.error('loadStats charges error:', err); }
  }

  chargeStatus(c: Charge): { label: string; cls: string } {
    if (c.statut === 'Paye') return { label: 'Payée', cls: 'paid' };
    if (c.statut === 'Partiel') return { label: 'Partiel', cls: 'partial' };
    if (c.statut === 'Annule') return { label: 'Annulée', cls: 'cancelled' };
    return { label: 'En attente', cls: 'pending' };
  }

  get filtered(): Charge[] { return this.charges; }
  get stats() { return this.statsData; }
  get total(): number { return this.totalCount; }
  get paged(): Charge[] { return this.charges; }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.categorieFilter = '';
    this.selectedDate = '';
    this.page = 1;
    this.load();
  }

  openEdit(c: Charge): void {
    this.editCharge = c;
    this.editTitre = c.titre;
    this.editDescription = c.description ?? '';
    this.editMontant = c.montant;
    this.editCategorieId = c.categorieChargeId;
    const d = new Date(c.dateCharge);
    this.editDateCharge = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
    this.editFournisseurId = c.fournisseurId ?? '';
    this.editJustificatif = c.justificatif ?? '';
    this.editEstRecurrente = c.estRecurrente;
    this.editPeriodicite = c.periodicite ?? 'Mensuelle';
    this.editSaving = false;
    this.editModalOpen = true;
  }

  async saveEdit(): Promise<void> {
    if (!this.editCharge) return;
    if (!this.editTitre.trim()) { this.toast.notify('Le titre est requis', 'warning'); return; }
    if (!this.editMontant || this.editMontant <= 0) { this.toast.notify('Le montant doit être > 0', 'warning'); return; }
    if (!this.editCategorieId) { this.toast.notify('Sélectionnez une catégorie', 'warning'); return; }

    this.editSaving = true;
    this.cdr.markForCheck();
    try {
      await this.api.chargeUpdate(this.editCharge.id, {
        titre: this.editTitre.trim(),
        description: this.editDescription.trim() || null,
        montant: +this.editMontant,
        categorieChargeId: Number(this.editCategorieId),
        dateCharge: this.editDateCharge || null,
        fournisseurId: this.editFournisseurId !== '' ? Number(this.editFournisseurId) : null,
        justificatif: this.editJustificatif.trim() || null,
        estRecurrente: this.editEstRecurrente,
        periodicite: this.editEstRecurrente ? this.editPeriodicite : null,
      });
      this.editModalOpen = false;
      this.editCharge = null;
      this.toast.notify('Charge modifiée avec succès', 'success');
      this.load();
    } catch (e: any) {
      this.toast.notify(e?.error?.message || 'Erreur lors de la modification', 'error');
    } finally {
      this.editSaving = false;
      this.cdr.markForCheck();
    }
  }

  async deleteCharge(c: Charge): Promise<void> {
    if (!confirm(`Supprimer la charge « ${c.titre } » (${this.formatNum(c.montant)} MAD) ?\n\nCette action est irréversible.`)) return;
    try {
      await this.api.chargeDelete(c.id);
      this.toast.notify('Charge supprimée', 'success');
      this.load();
    } catch (e: any) {
      this.toast.notify(e?.error?.message || 'Impossible de supprimer cette charge', 'error');
    }
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
    this.cdr.markForCheck();
    try {
      const updated = await this.api.chargeAddPaiement(this.viewCharge.id, {
        montant: this.paiementMontant,
        methode: this.paiementMethode,
      });
      this.paiementMontant = 0;
      this.toast.notify('Paiement enregistré', 'success');
      if (updated.statut === 'Paye') {
        this.modalOpen = false;
        this.viewCharge = null;
      } else {
        this.viewCharge = updated;
      }
      this.load();
    } catch {
      this.toast.notify('Erreur lors du paiement', 'error');
    } finally {
      this.paiementSaving = false;
      this.cdr.markForCheck();
    }
  }

  async genererRecurrentes(): Promise<void> {
    this.generatingRecurrentes = true;
    this.cdr.markForCheck();
    try {
      const result = await this.api.chargeGenererRecurrentes();
      this.toast.notify(result.message, result.count > 0 ? 'success' : 'info');
      if (result.count > 0) await this.load();
    } catch {
      this.toast.notify('Erreur lors de la génération des charges récurrentes', 'error');
    } finally {
      this.generatingRecurrentes = false;
      this.cdr.markForCheck();
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
    this.cdr.markForCheck();
    try {
      const created = await this.api.categorieChargeCreate({ nom: this.newCatNom.trim(), icone: this.newCatIcone });
      this.categories = [...this.categories, created].sort((a, b) => a.nom.localeCompare(b.nom));
      this.newCatModalOpen = false;
      this.toast.notify(`Catégorie « ${created.nom} » créée`, 'success');
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.newCatSaving = false; this.cdr.markForCheck(); }
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

}
