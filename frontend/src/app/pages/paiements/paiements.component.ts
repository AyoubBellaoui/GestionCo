import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { Paiement, Vente } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-paiements',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './paiements.component.html',
})
export class PaiementsComponent implements OnInit {
  paiements: Paiement[] = [];
  ventes: Vente[] = [];
  loading = true;
  search = '';
  methodeFilter = '';
  statutFilter = '';

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try {
      const [p, v] = await Promise.all([
        this.api.paiementsList().catch(() => []),
        this.api.ventesList().catch(() => []),
      ]);
      this.paiements = p;
      this.ventes = v;
    } finally { this.loading = false; }
  }

  get filtered(): Paiement[] {
    return this.paiements.filter(p => {
      if (this.search && !(p.venteReference || '').toLowerCase().includes(this.search.toLowerCase()) &&
        !p.nomClient.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.methodeFilter && p.methode !== this.methodeFilter) return false;
      if (this.statutFilter && p.statut !== this.statutFilter) return false;
      return true;
    });
  }

  get ventesEnAttente(): Vente[] {
    return this.ventes.filter(v => v.statut === 'EnAttente');
  }

  venteStatut(v: Vente): { label: string; cls: string } {
    if (v.montantPaye > 0) return { label: 'Partiel', cls: 'partial' };
    return { label: 'En attente', cls: 'pending' };
  }

  get mergedRows(): Array<{ kind: 'paiement'; p: Paiement } | { kind: 'vente'; v: Vente }> {
    const paiementRows = this.filtered.map(p => ({ kind: 'paiement' as const, p }));

    // Get ventes that are unpaid AND don't have an associated payment in the filtered list
    const ventesWithPayments = new Set(paiementRows.map(row => row.p.venteId));
    const venteRows = this.ventesEnAttente
      .filter(v => !ventesWithPayments.has(v.id))
      .map(v => ({ kind: 'vente' as const, v }));

    return [...paiementRows, ...venteRows].sort((a, b) => {
      const da = a.kind === 'paiement' ? a.p.datePaiement : a.v.dateVente;
      const db = b.kind === 'paiement' ? b.p.datePaiement : b.v.dateVente;
      return new Date(db).getTime() - new Date(da).getTime();
    });
  }

  isPaiement(row: { kind: string }): row is { kind: 'paiement'; p: Paiement } {
    return row.kind === 'paiement';
  }

  get stats() {
    const total = this.paiements.reduce((s, p) => s + p.montant, 0);
    const payes = this.paiements.filter(p => p.statut === 'Confirme').reduce((s, p) => s + p.montant, 0);
    const enAttente = this.paiements.filter(p => p.statut === 'EnAttente').length;
    return { total, payes, enAttente, count: this.paiements.length };
  }

  methodIcon(m: string): string {
    const map: Record<string, string> = { 'Especes': '💵', 'Virement': '🏦', 'Cheque': '📝', 'CarteBancaire': '💳' };
    return map[m] || '💰';
  }

  statutCls(s: string): string {
    if (s === 'Confirme') return 'good';
    if (s === 'EnAttente') return 'medium';
    return 'low';
  }

  statutLabel(s: string): string {
    if (s === 'Confirme') return 'Payé';
    if (s === 'EnAttente') return 'En attente';
    if (s === 'Annule') return 'Annulé';
    return s;
  }

  getVenteById(venteId: number): Vente | undefined {
    return this.ventes.find(v => v.id === venteId);
  }

  getPaiementReste(p: Paiement): number {
    const vente = this.getVenteById(p.venteId);
    return vente ? vente.reste : 0;
  }

  resetFilters(): void {
    this.search = '';
    this.methodeFilter = '';
    this.statutFilter = '';
  }
}
