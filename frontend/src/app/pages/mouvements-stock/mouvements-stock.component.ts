import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { DateRangeComponent } from '../../shared/date-range/date-range.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { MouvementStock } from '../../core/models';
import { formatNum, formatDate } from '../../core/utils/format';

@Component({
  selector: 'app-mouvements-stock',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass, DateRangeComponent],
  templateUrl: './mouvements-stock.component.html',
})
export class MouvementsStockComponent implements OnInit {
  mouvements: MouvementStock[] = [];
  totalCount = 0;
  loading = true;
  search = '';
  dateFrom = '';
  dateTo = '';
  typeFilter = '';
  page = 1;
  pageSize = 25;
  statsData = { entrees: 0, sorties: 0, ajustements: 0, total: 0 };
  private searchTimer: any;

  formatNum = formatNum;
  formatDate = formatDate;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats()]); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.mouvementsListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        type: this.typeFilter || undefined,
        dateDebut: this.dateFrom || undefined,
        dateFin: this.dateTo || undefined,
      });
      this.mouvements = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.mouvementsStats();
      this.statsData = { entrees: s.entrees, sorties: s.sorties, ajustements: 0, total: s.total };
    } catch (err) { console.error('loadStats mouvements error:', err); }
  }

  get paged(): MouvementStock[] { return this.mouvements; }
  get filtered(): MouvementStock[] { return this.mouvements; }

  get stats() { return this.statsData; }

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

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  Math = Math;

  resetFilters(): void { this.search = ''; this.dateFrom = ''; this.dateTo = ''; this.typeFilter = ''; this.page = 1; this.load(); }
}
