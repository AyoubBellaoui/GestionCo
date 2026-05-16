import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { MouvementStock } from '../../core/models';
import { formatNum, formatDate } from '../../core/utils/format';

@Component({
  selector: 'app-mouvements-stock',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './mouvements-stock.component.html',
})
export class MouvementsStockComponent implements OnInit {
  mouvements: MouvementStock[] = [];
  loading = true;
  search = '';
  selectedDate = '';
  typeFilter = '';
  page = 1;
  pageSize = 25;

  formatNum = formatNum;
  formatDate = formatDate;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try { this.mouvements = await this.api.mouvementsList().catch(() => []); }
    finally { this.loading = false; }
  }

  get paged(): MouvementStock[] {
    return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize);
  }

  get filtered(): MouvementStock[] {
    return this.mouvements.filter(m => {
      if (this.search && !m.nomProduit.toLowerCase().includes(this.search.toLowerCase()) &&
        !m.referenceProduit.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.typeFilter && m.type !== this.typeFilter) return false;
      if (this.selectedDate) {
        const d = new Date(m.dateMouvement);
        const dStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        if (dStr !== this.selectedDate) return false;
      }
      return true;
    });
  }

  get stats() {
    return {
      entrees: this.mouvements.filter(m => m.type === 'Entree').length,
      sorties: this.mouvements.filter(m => m.type === 'Sortie').length,
      ajustements: this.mouvements.filter(m => m.type === 'Ajustement').length,
      total: this.mouvements.length,
    };
  }

  typeInfo(type: string): { label: string; cls: string; icon: string } {
    const map: Record<string, { label: string; cls: string; icon: string }> = {
      'Entree': { label: 'Entrée', cls: 'good', icon: '📥' },
      'Sortie': { label: 'Sortie', cls: 'low', icon: '📤' },
      'Ajustement': { label: 'Ajustement', cls: 'medium', icon: '⚖️' },
      'VenteClient': { label: 'Vente', cls: 'low', icon: '🛒' },
      'AchatFournisseur': { label: 'Achat', cls: 'good', icon: '📦' },
    };
    return map[type] || { label: type, cls: 'pending', icon: '📊' };
  }

  isPositif(type: string): boolean {
    return type === 'Entree' || type === 'AchatFournisseur';
  }

  Math = Math;

  resetFilters(): void { this.search = ''; this.selectedDate = ''; this.typeFilter = ''; this.page = 1; }
}
