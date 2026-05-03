import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
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

  formatNum = formatNum;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

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

  resetFilters(): void { this.search = ''; this.categorieFilter = ''; this.stockFilter = ''; }
}
