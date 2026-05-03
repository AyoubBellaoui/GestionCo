import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule, NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Vente, Client } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus, statusInfo } from '../../core/utils/format';

type DateRange = 'today' | '7d' | '30d' | '12m' | 'all';

@Component({
  selector: 'app-ventes',
  standalone: true,
  imports: [CommonModule, TopbarComponent, ModalComponent, FormsModule, NgClass],
  templateUrl: './ventes.component.html',
})
export class VentesComponent implements OnInit {
  ventes: Vente[] = [];
  clients: Client[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  clientFilter = '';
  dateRange: DateRange = 'all';
  page = 1;
  pageSize = 10;
  modalOpen = false;
  viewVente: Vente | null = null;
  selectedDate = '';

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  statusInfo = statusInfo;

  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [v, c] = await Promise.all([
        this.api.ventesList().catch(() => []),
        this.api.clientsList().catch(() => []),
      ]);
      this.ventes = v;
      this.clients = c;
    } finally { this.loading = false; }
  }

  venteStatus(v: Vente): { label: string; cls: string } {
    if (v.statut === 'EnAttente' && v.montantPaye > 0) return { label: 'Partiel', cls: 'partial' };
    return statusInfo(v.statut);
  }

  get filtered(): Vente[] {
    const now = Date.now();
    const ranges: Record<DateRange, number> = { today: 86400000, '7d': 7*86400000, '30d': 30*86400000, '12m': 365*86400000, all: Infinity };
    const rangeMs = ranges[this.dateRange];
    return this.ventes.filter(v => {
      if (this.search && !(v.reference.toLowerCase().includes(this.search.toLowerCase()) || (v.nomClient || '').toLowerCase().includes(this.search.toLowerCase()))) return false;
      if (this.statusFilter === 'Partiel') {
        if (!(v.statut === 'EnAttente' && v.montantPaye > 0)) return false;
      } else if (this.statusFilter && v.statut !== this.statusFilter) {
        return false;
      }
      if (this.clientFilter && String(v.clientId) !== this.clientFilter) return false;
      if (this.selectedDate) {
        if (this.formatIsoDate(new Date(v.dateVente)) !== this.selectedDate) return false;
      } else if (rangeMs !== Infinity && now - new Date(v.dateVente).getTime() > rangeMs) return false;
      return true;
    });
  }

  get venteDates(): string[] {
    return Array.from(new Set(this.ventes.map(v => this.formatIsoDate(new Date(v.dateVente))))).sort();
  }

  get stats() {
    const now = new Date();
    const thisMonth = this.ventes.filter(v => { const d = new Date(v.dateVente); return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear(); });
    const ca = thisMonth.reduce((s, v) => s + v.montantTotal, 0);
    const count = thisMonth.length;
    const panier = count > 0 ? ca / count : 0;
    const impayes = this.ventes.filter(v => v.statut === 'EnAttente').reduce((s, v) => s + v.reste, 0);
    const nbImpayes = this.ventes.filter(v => v.statut === 'EnAttente' && v.reste > 0).length;
    return { ca, count, panier, impayes, nbImpayes };
  }


  get total(): number { return this.filtered.length; }
  get pageCount(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }
  get paged(): Vente[] { return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize); }

  setDateRange(r: string): void { this.dateRange = r as DateRange; this.page = 1; }
  resetFilters(): void {
    this.search = '';
    this.statusFilter = '';
    this.clientFilter = '';
    this.dateRange = '30d';
    this.selectedDate = '';
    this.page = 1;
  }

  formatIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }


  openDetail(v: Vente): void { this.viewVente = v; this.modalOpen = true; }

  async handleCancel(id: number): Promise<void> {
    if (!confirm('Annuler cette vente ?')) return;
    try { await this.api.venteCancel(id); this.toast.notify('Vente annulée', 'success'); await this.load(); }
    catch { this.toast.notify("Impossible d'annuler la vente", 'error'); }
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
