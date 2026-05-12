import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { ExportService } from '../../core/services/export.service';
import { AuthService } from '../../core/services/auth.service';
import { Produit } from '../../core/models';
import { formatNum } from '../../core/utils/format';

@Component({
  selector: 'app-produits',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './produits.component.html',
})
export class ProduitsComponent implements OnInit {
  produits: Produit[] = [];
  loading = true;
  search = '';
  categorieFilter = '';
  stockFilter = '';

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

  exportExcel(): void { this.exportSvc.exportProduits(this.produits); }

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.produits = await this.api.produitsList().catch(() => []); }
    finally { this.loading = false; }
  }

  get categories(): string[] {
    return Array.from(new Set(this.produits.map(p => p.categorieNom).filter(Boolean))) as string[];
  }

  get filtered(): Produit[] {
    return this.produits.filter(p => {
      if (this.search && !p.reference?.toLowerCase().includes(this.search.toLowerCase()) && !p.nom.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.categorieFilter && p.categorieNom !== this.categorieFilter) return false;
      if (this.stockFilter === 'low' && p.quantiteStock > p.seuilAlerte) return false;
      if (this.stockFilter === 'out' && p.quantiteStock > 0) return false;
      if (this.stockFilter === 'ok' && p.quantiteStock <= p.seuilAlerte) return false;
      return true;
    });
  }

  get stats() {
    return {
      total: this.produits.length,
      valeur: this.produits.reduce((s, p) => s + p.quantiteStock * p.prixHT, 0),
      faible: this.produits.filter(p => p.isStockFaible).length,
      rupture: this.produits.filter(p => p.isRupture).length,
    };
  }

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

  resetFilters(): void { this.search = ''; this.categorieFilter = ''; this.stockFilter = ''; }
}
