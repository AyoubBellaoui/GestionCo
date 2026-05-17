import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';
import { AuthService } from '../../core/services/auth.service';
import { Produit, Categorie } from '../../core/models';
import { formatNum } from '../../core/utils/format';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-produits',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './produits.component.html',
})
export class ProduitsComponent implements OnInit {
  produits: Produit[] = [];
  categories: Categorie[] = [];
  totalCount = 0;
  loading = true;
  search = '';
  categorieFilter = '';
  stockFilter = '';
  page = 1;
  pageSize = 15;
  statsData = { total: 0, valeur: 0, faible: 0, rupture: 0 };
  private searchTimer: any;

  // Detail modal
  detailModal = false;
  detailProduit: Produit | null = null;

  // Ajustement modal
  ajustModal = false;
  ajustProduit: Produit | null = null;
  ajustForm = { type: 'Entree', quantite: 1, raison: '', commentaire: '' };
  ajustSaving = false;

  formatNum = formatNum;

  constructor(private api: ApiService, private toast: ToastService, private exportSvc: ExportService, public router: Router, public auth: AuthService) {}

  importProgress = '';
  importRunning = false;
  importResult: { imported: number; failed: number; errors: { row: number; message: string }[] } | null = null;

  exportExcel(): void { this.exportSvc.exportProduits(this.produits); }

  downloadTemplate(): void {
    const ws = XLSX.utils.aoa_to_sheet([
      ['Nom', 'Description', 'Prix Achat HT', 'TVA (%)', 'Prix Vente HT', 'TVA Vente (%)', 'Stock', 'Seuil Alerte'],
      ['Exemple Produit', 'Description optionnelle', 100, 20, 150, 20, 10, 3],
    ]);
    ws['!cols'] = [{ wch: 25 }, { wch: 30 }, { wch: 14 }, { wch: 8 }, { wch: 14 }, { wch: 14 }, { wch: 8 }, { wch: 12 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Produits');
    XLSX.writeFile(wb, 'modele-import-produits.xlsx');
  }

  triggerImport(): void {
    const input = document.getElementById('import-produits') as HTMLInputElement;
    input?.click();
  }

  async onImportFile(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.importRunning = true;
    this.importProgress = 'Lecture du fichier…';
    try {
      const data = await file.arrayBuffer();
      const wb = XLSX.read(data);
      const rows: any[] = XLSX.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]]);
      if (rows.length === 0) { this.toast.notify('Fichier vide ou format incorrect', 'warning'); return; }
      if (rows.length > 500) { this.toast.notify('Maximum 500 lignes par import', 'warning'); return; }

      this.importProgress = `Envoi de ${rows.length} ligne(s)…`;

      const items = rows.map(r => ({
        nom: String(r['Nom'] || r['nom'] || r['NOM'] || ''),
        description: r['Description'] || r['description'] || '',
        prixHT: +(r['Prix Achat HT'] || r['Prix HT'] || r['prixHT'] || 0),
        tva: +(r['TVA (%)'] || r['TVA'] || r['tva'] || 20),
        prixVenteHT: +(r['Prix Vente HT'] || r['prixVenteHT'] || 0),
        tvaVente: +(r['TVA Vente (%)'] || r['TVA Vente'] || r['tvaVente'] || 20),
        quantiteStock: +(r['Stock'] || r['stock'] || r['quantiteStock'] || 0),
        seuilAlerte: +(r['Seuil Alerte'] || r['seuilAlerte'] || 5),
        isActive: true,
      }));

      const result = await this.api.produitBulkImport(items);
      await this.load();
      this.importResult = result;
      const msg = `Import terminé : ${result.imported} produit(s) créé(s)${result.failed > 0 ? ', ' + result.failed + ' erreur(s)' : ''}`;
      this.toast.notify(msg, result.imported > 0 ? 'success' : 'warning');
    } catch { this.toast.notify('Erreur lors de la lecture du fichier', 'error'); }
    finally { this.importRunning = false; this.importProgress = ''; (event.target as HTMLInputElement).value = ''; }
  }

  async ngOnInit(): Promise<void> {
    const [, , cats] = await Promise.all([
      this.load(),
      this.loadStats(),
      this.api.categoriesList().catch(() => []),
    ]);
    this.categories = cats;
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.produitsListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        categorieId: this.categorieFilter ? +this.categorieFilter : undefined,
        stockFaibleOnly: this.stockFilter === 'low' ? true : undefined,
        ruptureOnly: this.stockFilter === 'out' ? true : undefined,
        disponibleOnly: this.stockFilter === 'ok' ? true : undefined,
      });
      this.produits = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.produitsStats();
      this.statsData = { total: s.totalProduits, valeur: s.valeurTotaleStock, faible: s.produitsStockFaible, rupture: s.produitsRupture };
    } catch {}
  }

  get paged(): Produit[] { return this.produits; }
  get filtered(): Produit[] { return this.produits; }

  get stats() {
    return {
      total: this.statsData.total,
      valeur: this.statsData.valeur,
      faible: this.statsData.faible,
      rupture: this.statsData.rupture,
    };
  }

  onFilterChange(): void { this.page = 1; this.load(); }
  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  stockStatus(p: Produit): { cls: string; label: string } {
    if (p.isRupture) return { cls: 'low', label: 'Rupture' };
    if (p.isStockFaible) return { cls: 'medium', label: 'Faible' };
    return { cls: 'good', label: 'Disponible' };
  }

  async handleDelete(id: number): Promise<void> {
    if (!confirm('Supprimer ce produit ?')) return;
    try { await this.api.produitDelete(id); this.toast.notify('Produit supprimé', 'success'); this.load(); }
    catch { this.toast.notify('Erreur de suppression', 'error'); }
  }

  get detailMarge(): number { return (this.detailProduit?.prixVenteHT ?? 0) - (this.detailProduit?.prixHT ?? 0); }
  get detailMargePct(): number {
    const achat = this.detailProduit?.prixHT ?? 0;
    return achat > 0 ? Math.round((this.detailMarge / achat) * 100) : 0;
  }

  openDetail(p: Produit): void {
    this.detailProduit = p;
    this.detailModal = true;
  }

  closeDetail(): void {
    this.detailModal = false;
    this.detailProduit = null;
  }

  editDetail(): void {
    const id = this.detailProduit?.id;
    this.closeDetail();
    if (id) this.router.navigate(['/produits', id, 'modifier']);
  }

  openAjust(p: Produit): void {
    this.ajustProduit = p;
    this.ajustForm = { type: 'Entree', quantite: 1, raison: '', commentaire: '' };
    this.ajustModal = true;
  }

  closeAjust(): void {
    this.ajustModal = false;
    this.ajustProduit = null;
  }

  get ajustNewStock(): number {
    if (!this.ajustProduit) return 0;
    const q = this.ajustForm.quantite || 0;
    return this.ajustForm.type === 'Entree'
      ? this.ajustProduit.quantiteStock + q
      : Math.max(0, this.ajustProduit.quantiteStock - q);
  }

  async saveAjust(): Promise<void> {
    if (!this.ajustProduit) return;
    if ((this.ajustForm.quantite ?? 0) <= 0) { this.toast.notify('La quantité doit être > 0', 'warning'); return; }
    if (!this.ajustForm.raison.trim()) { this.toast.notify('La raison est obligatoire', 'warning'); return; }
    if (this.ajustForm.type === 'Sortie' && this.ajustForm.quantite > this.ajustProduit.quantiteStock) {
      this.toast.notify(`Stock insuffisant (stock actuel : ${this.ajustProduit.quantiteStock})`, 'warning'); return;
    }
    this.ajustSaving = true;
    try {
      await this.api.ajustementStock({
        produitId: this.ajustProduit.id,
        type: this.ajustForm.type,
        quantite: this.ajustForm.quantite,
        raison: this.ajustForm.raison,
        commentaire: this.ajustForm.commentaire || undefined,
      });
      this.toast.notify(`Stock ajusté : ${this.ajustForm.type === 'Entree' ? '+' : '-'}${this.ajustForm.quantite} unités`, 'success');
      this.closeAjust();
      await this.load();
    } catch { this.toast.notify("Erreur lors de l'ajustement", 'error'); }
    finally { this.ajustSaving = false; }
  }

  resetFilters(): void { this.search = ''; this.categorieFilter = ''; this.stockFilter = ''; this.page = 1; this.load(); }

  reapproLoading = new Set<number>();

  async genererReappro(p: Produit): Promise<void> {
    if (this.reapproLoading.has(p.id)) return;
    this.reapproLoading.add(p.id);
    try {
      const result = await this.api.produitGenererReappro(p.id);
      if (result.created) {
        this.toast.notify(`Bon de commande ${result.reference} créé pour ${p.nom}`, 'success');
      } else {
        this.toast.notify(result.message || 'Réapprovisionnement déjà en cours', 'info');
      }
    } catch { this.toast.notify('Erreur lors du réapprovisionnement', 'error'); }
    finally { this.reapproLoading.delete(p.id); }
  }
}
