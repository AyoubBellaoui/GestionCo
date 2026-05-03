import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { MouvementStock } from '../../core/models';
import { formatNum, formatDate } from '../../core/utils/format';

@Component({
  selector: 'app-mouvements-stock',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './mouvements-stock.component.html',
})
export class MouvementsStockComponent implements OnInit {
  mouvements: MouvementStock[] = [];
  loading = true;
  search = '';
  typeFilter = '';

  formatNum = formatNum;
  formatDate = formatDate;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try { this.mouvements = await this.api.mouvementsList().catch(() => []); }
    finally { this.loading = false; }
  }

  get filtered(): MouvementStock[] {
    return this.mouvements.filter(m => {
      if (this.search && !m.nomProduit.toLowerCase().includes(this.search.toLowerCase()) &&
        !m.referenceProduit.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.typeFilter && m.type !== this.typeFilter) return false;
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

  resetFilters(): void { this.search = ''; this.typeFilter = ''; }
}
