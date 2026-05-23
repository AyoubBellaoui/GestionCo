import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule, NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Commande, Client } from '../../core/models';
import { formatNum, formatDate, getAvatarClass } from '../../core/utils/format';
import { SettingsService } from '../../core/services/settings.service';

@Component({
  selector: 'app-commandes',
  standalone: true,
  imports: [CommonModule, TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './commandes.component.html',
})
export class CommandesComponent implements OnInit {
  commandesList: Commande[] = [];
  totalCount = 0;
  clients: Client[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  clientFilter = '';
  selectedDate = '';
  page = 1;
  pageSize = 10;
  statsData = { total: 0, enAttente: 0, confirmees: 0, converties: 0, montantTotal: 0 };
  private searchTimer: any;

  formatNum = formatNum;
  formatDate = formatDate;
  getAvatarClass = getAvatarClass;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats(), this.loadClients()]); }

  private async loadClients(): Promise<void> {
    try { this.clients = await this.api.clientsList().catch(() => []); } catch {}
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.commandesListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        statut: this.statusFilter || undefined,
        clientId: this.clientFilter ? Number(this.clientFilter) : undefined,
        dateDebut: this.selectedDate || undefined,
        dateFin: this.selectedDate || undefined,
      });
      this.commandesList = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try { this.statsData = await this.api.commandeStats(); } catch {}
  }

  statutInfo(c: Commande): { label: string; cls: string } {
    switch (c.statut) {
      case 'EnAttente':  return { label: 'En attente', cls: 'neutral' };
      case 'Confirmee':  return { label: 'Confirmée', cls: 'medium' };
      case 'EnCours':    return { label: 'En cours', cls: 'medium' };
      case 'Livree':     return { label: 'Livrée', cls: 'good' };
      case 'Convertie':  return { label: 'Convertie ✓', cls: 'partial' };
      case 'Annulee':    return { label: 'Annulée', cls: 'bad' };
      default:           return { label: c.statut, cls: 'neutral' };
    }
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }
  resetFilters(): void { this.search = ''; this.statusFilter = ''; this.clientFilter = ''; this.selectedDate = ''; this.page = 1; this.load(); }

  async convertirEnVente(c: Commande): Promise<void> {
    if (!confirm(`Convertir la commande ${c.reference} en vente ?`)) return;
    this.router.navigate(['/ventes/nouvelle'], { queryParams: { commandeId: c.id } });
  }

  async changerStatut(c: Commande, statut: string): Promise<void> {
    const labels: Record<string, string> = { Confirmee: 'confirmée', EnCours: 'en cours', Livree: 'livrée', Annulee: 'annulée' };
    if (!confirm(`Marquer la commande ${c.reference} comme ${labels[statut] || statut} ?`)) return;
    try {
      await this.api.commandeUpdateStatut(c.id, statut);
      this.toast.notify(`Commande ${labels[statut] || statut}`, 'success');
      this.load();
    } catch (e: any) { this.toast.notify(e?.error?.message || 'Erreur lors de la mise à jour', 'error'); }
  }

  async supprimer(c: Commande): Promise<void> {
    if (!confirm(`Supprimer la commande ${c.reference} ?`)) return;
    try {
      await this.api.commandeDelete(c.id);
      this.toast.notify('Commande supprimée', 'success');
      this.load();
    } catch (e: any) { this.toast.notify(e?.error?.message || 'Erreur lors de la suppression', 'error'); }
  }

  async duplicate(c: Commande): Promise<void> {
    try {
      const full = await this.api.commandeGet(c.id);
      this.router.navigate(['/commandes/nouvelle'], { state: { duplicate: full } });
    } catch { this.toast.notify('Erreur lors de la duplication', 'error'); }
  }

  get devise(): string { return this.settings.settings.devise || 'MAD'; }
}
