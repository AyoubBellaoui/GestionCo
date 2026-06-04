import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { DateRangeComponent } from '../../shared/date-range/date-range.component';
import { ApiService } from '../../core/services/api.service';
import { Paiement, PaiementAchat, Vente, Achat } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus } from '../../core/utils/format';

@Component({
  selector: 'app-paiements',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass, DateRangeComponent],
  templateUrl: './paiements.component.html',
})
export class PaiementsComponent implements OnInit {
  activeTab: 'ventes' | 'achats' = 'ventes';

  paiements: Paiement[] = [];
  ventes: Vente[] = [];
  paiementsAchat: PaiementAchat[] = [];
  achats: Achat[] = [];
  loading = true;

  search = '';
  dateFrom = '';
  dateTo = '';
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
      const [p, v, pa, a] = await Promise.all([
        this.api.paiementsList().catch(() => []),
        this.api.ventesList().catch(() => []),
        this.api.paiementsAchatList().catch(() => []),
        this.api.achatsList().catch(() => []),
      ]);
      this.paiements = p;
      this.ventes = v;
      this.paiementsAchat = pa;
      this.achats = a;
    } finally { this.loading = false; }
  }

  setTab(tab: 'ventes' | 'achats'): void {
    this.activeTab = tab;
    this.resetFilters();
  }

  // ── VENTES tab ──

  private venteActive(venteId: number): boolean {
    const v = this.ventes.find(v => v.id === venteId);
    return v?.statut !== 'Annule';
  }

  get filtered(): Paiement[] {
    return this.paiements.filter(p => {
      if (!this.venteActive(p.venteId)) return false;
      if (this.search && !(p.venteReference || '').toLowerCase().includes(this.search.toLowerCase()) &&
        !p.nomClient.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.methodeFilter && p.methode !== this.methodeFilter) return false;
      if (this.statutFilter && p.statut !== this.statutFilter) return false;
      if (this.dateFrom || this.dateTo) {
        const d = new Date(p.datePaiement);
        const dStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        if (this.dateFrom && dStr < this.dateFrom) return false;
        if (this.dateTo && dStr > this.dateTo) return false;
      }
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
    const active = this.paiements.filter(p => this.venteActive(p.venteId));
    const total = active.reduce((s, p) => s + p.montant, 0);
    const payes = active.filter(p => p.statut === 'Confirme').reduce((s, p) => s + p.montant, 0);
    const enAttente = this.ventes.filter(v => v.statut === 'EnAttente' && v.montantPaye === 0).length;
    return { total, payes, enAttente, count: active.length };
  }

  getVenteById(venteId: number): Vente | undefined {
    return this.ventes.find(v => v.id === venteId);
  }

  getPaiementReste(p: Paiement): number {
    const vente = this.getVenteById(p.venteId);
    return vente ? vente.reste : 0;
  }

  // ── ACHATS tab ──

  get filteredAchatsPayments(): PaiementAchat[] {
    return this.paiementsAchat.filter(p => {
      if (this.search && !(p.achatReference || '').toLowerCase().includes(this.search.toLowerCase()) &&
        !p.nomFournisseur.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.methodeFilter && p.methode !== this.methodeFilter) return false;
      if (this.statutFilter && p.statut !== this.statutFilter) return false;
      if (this.dateFrom || this.dateTo) {
        const d = new Date(p.datePaiement);
        const dStr = `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
        if (this.dateFrom && dStr < this.dateFrom) return false;
        if (this.dateTo && dStr > this.dateTo) return false;
      }
      return true;
    });
  }

  get achatsNonSoldes(): Achat[] {
    return this.achats.filter(a => a.statut !== 'Annule' && a.reste > 0);
  }

  achatStatut(a: Achat): { label: string; cls: string } {
    if (a.statut === 'Paye') return { label: 'Payé', cls: 'good' };
    if (a.montantPaye > 0) return { label: 'Partiel', cls: 'partial' };
    return { label: 'En attente', cls: 'pending' };
  }

  get mergedAchatRows(): Array<{ kind: 'paiement'; p: PaiementAchat } | { kind: 'achat'; a: Achat }> {
    const paiementRows = this.filteredAchatsPayments.map(p => ({ kind: 'paiement' as const, p }));
    const achatsWithPayments = new Set(paiementRows.map(row => row.p.achatId));
    // Show ALL achats not yet represented by a paiement row (any status except Annule)
    const achatRows = this.achats
      .filter(a => a.statut !== 'Annule' && !achatsWithPayments.has(a.id))
      .map(a => ({ kind: 'achat' as const, a }));
    return [...paiementRows, ...achatRows].sort((a, b) => {
      const da = a.kind === 'paiement' ? a.p.datePaiement : a.a.dateAchat;
      const db = b.kind === 'paiement' ? b.p.datePaiement : b.a.dateAchat;
      return new Date(db).getTime() - new Date(da).getTime();
    });
  }

  isPaiementAchat(row: { kind: string }): row is { kind: 'paiement'; p: PaiementAchat } {
    return row.kind === 'paiement';
  }

  get statsAchats() {
    const actifs = this.achats.filter(a => a.statut !== 'Annule');
    const total = actifs.reduce((s, a) => s + a.montantPaye, 0);
    const payes = actifs.filter(a => a.statut === 'Paye').reduce((s, a) => s + a.montantPaye, 0);
    const enAttente = actifs.filter(a => a.statut === 'EnAttente').length;
    const nonSoldes = actifs.filter(a => a.reste > 0).length;
    return { total, payes, enAttente, count: actifs.length, nonSoldes };
  }

  getAchatById(achatId: number): Achat | undefined {
    return this.achats.find(a => a.id === achatId);
  }

  getPaiementAchatReste(p: PaiementAchat): number {
    const achat = this.getAchatById(p.achatId);
    return achat ? achat.reste : 0;
  }

  // ── SHARED ──

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

  resetFilters(): void {
    this.search = '';
    this.dateFrom = ''; this.dateTo = '';
    this.methodeFilter = '';
    this.statutFilter = '';
  }
}
