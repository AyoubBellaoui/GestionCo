import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass, DecimalPipe, DatePipe } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { PLReport, TVAReport, BalanceAgeeReport, BalanceAgeeClient, PerformanceCommerciale } from '../../core/models';
import { formatNum } from '../../core/utils/format';
import * as XLSX from 'xlsx';

type Tab = 'pl' | 'tva' | 'cashflow' | 'balance' | 'performance';

@Component({
  selector: 'app-rapports',
  standalone: true,
  imports: [FormsModule, NgClass, DecimalPipe, DatePipe, TopbarComponent],
  templateUrl: './rapports.component.html',
})
export class RapportsComponent implements OnInit {
  tab: Tab = 'pl';
  annee = new Date().getFullYear();
  annees: number[] = [];

  plReport: PLReport | null = null;
  tvaReport: TVAReport | null = null;
  cashFlow: any = null;
  cfMois = new Date().getMonth() + 1;
  balanceAgee: BalanceAgeeReport | null = null;
  performance: PerformanceCommerciale | null = null;
  loading = false;

  readonly moisLabels = ['Janvier','Février','Mars','Avril','Mai','Juin','Juillet','Août','Septembre','Octobre','Novembre','Décembre'];

  formatNum = formatNum;

  constructor(private api: ApiService) {
    const current = new Date().getFullYear();
    for (let y = current; y >= current - 4; y--) this.annees.push(y);
  }

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [pl, tva, cf, balance, perf] = await Promise.allSettled([
        this.api.rapportPL(this.annee),
        this.api.rapportTVA(this.annee),
        this.api.rapportCashFlow(this.annee, this.cfMois),
        this.api.rapportBalanceAgee(),
        this.api.rapportPerformance(this.annee),
      ]);
      if (pl.status       === 'fulfilled') this.plReport    = pl.value;
      if (tva.status      === 'fulfilled') this.tvaReport   = tva.value;
      if (cf.status       === 'fulfilled') this.cashFlow    = cf.value;
      if (balance.status  === 'fulfilled') this.balanceAgee = balance.value;
      if (perf.status     === 'fulfilled') this.performance = perf.value;
    } finally { this.loading = false; }
  }

  async onAnneeChange(): Promise<void> { await this.load(); }

  async onCfMoisChange(): Promise<void> {
    this.loading = true;
    try { this.cashFlow = await this.api.rapportCashFlow(this.annee, this.cfMois); }
    finally { this.loading = false; }
  }

  cfAllZero(): boolean {
    return !this.cashFlow || this.cashFlow.jours.every((j: any) => j.entrees === 0 && j.sorties === 0);
  }

  cfBarMax(): number {
    if (!this.cashFlow) return 1;
    return Math.max(1, ...this.cashFlow.jours.map((j: any) => Math.max(j.entrees, j.sorties)));
  }
  cfBar(val: number): number { return Math.round((val / this.cfBarMax()) * 100); }

  // ── Balance Âgée helpers ──────────────────────────────────────
  agingBarMax(): number {
    if (!this.balanceAgee?.clients.length) return 1;
    return Math.max(1, ...this.balanceAgee.clients.map(c => c.totalImpaye));
  }
  agingBar(val: number): number { return Math.round((val / this.agingBarMax()) * 100); }

  agingBucketPct(val: number): number {
    if (!this.balanceAgee || this.balanceAgee.totalImpaye === 0) return 0;
    return Math.round((val / this.balanceAgee.totalImpaye) * 100);
  }

  agingRiskClass(c: BalanceAgeeClient): string {
    if (c.j90Plus > 0)  return 'critical';
    if (c.j61_90 > 0)   return 'high';
    if (c.j31_60 > 0)   return 'medium';
    if (c.j1_30 > 0)    return 'low';
    return '';
  }

  // ── Performance helpers ───────────────────────────────────────
  perfClientBarMax(): number {
    if (!this.performance?.topClients.length) return 1;
    return Math.max(1, ...this.performance.topClients.map(c => c.montantTotal));
  }
  perfClientBar(val: number): number { return Math.round((val / this.perfClientBarMax()) * 100); }

  perfProduitBarMax(): number {
    if (!this.performance?.topProduits.length) return 1;
    return Math.max(1, ...this.performance.topProduits.map(p => p.montantHT));
  }
  perfProduitBar(val: number): number { return Math.round((val / this.perfProduitBarMax()) * 100); }

  // ── Excel export ──────────────────────────────────────────────
  exportExcel(): void {
    if (this.tab === 'pl' && this.plReport) {
      const rows = this.plReport.mois.map(m => ({
        'Mois':               m.nomMois,
        'CA HT (MAD)':        m.revenuHT,
        'TVA Collectée':      m.revenuTVA,
        'CA TTC (MAD)':       m.revenuTTC,
        'Coût Achat (MAD)':   m.coutAchat,
        'Charges Op. (MAD)':  m.chargesOp,
        'Résultat Brut':      m.resultatBrut,
        'Résultat Net':       m.resultatNet,
      }));
      rows.push({
        'Mois':               'TOTAL',
        'CA HT (MAD)':        this.plReport.totalRevenuHT,
        'TVA Collectée':      this.plReport.totalRevenuTVA,
        'CA TTC (MAD)':       this.plReport.totalRevenuTTC,
        'Coût Achat (MAD)':   this.plReport.totalCoutAchat,
        'Charges Op. (MAD)':  this.plReport.totalChargesOp,
        'Résultat Brut':      this.plReport.totalResultatBrut,
        'Résultat Net':       this.plReport.totalResultatNet,
      });
      const ws = XLSX.utils.json_to_sheet(rows);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, `P&L ${this.annee}`);
      XLSX.writeFile(wb, `rapport-pl-${this.annee}.xlsx`);

    } else if (this.tab === 'tva' && this.tvaReport) {
      const rows = this.tvaReport.mois.map(m => ({
        'Mois':                m.nomMois,
        'TVA Collectée (MAD)': m.tvaCollectee,
        'TVA Déductible (MAD)':m.tvaDeductible,
        'TVA Nette (MAD)':     m.tvaNette,
        'Situation':           m.tvaNette > 0 ? 'À reverser' : m.tvaNette < 0 ? 'Crédit TVA' : '—',
      }));
      rows.push({
        'Mois':                'TOTAL',
        'TVA Collectée (MAD)': this.tvaReport.totalTVACollectee,
        'TVA Déductible (MAD)':this.tvaReport.totalTVADeductible,
        'TVA Nette (MAD)':     this.tvaReport.totalTVANette,
        'Situation':           this.tvaReport.totalTVANette > 0 ? 'À reverser' : this.tvaReport.totalTVANette < 0 ? 'Crédit TVA' : 'Équilibre',
      });
      const ws = XLSX.utils.json_to_sheet(rows);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, `TVA ${this.annee}`);
      XLSX.writeFile(wb, `rapport-tva-${this.annee}.xlsx`);

    } else if (this.tab === 'balance' && this.balanceAgee) {
      const rows = this.balanceAgee.clients.map(c => ({
        'Client':          c.nomClient,
        'Total impayé':    c.totalImpaye,
        'Courant':         c.courant,
        '1-30 jours':      c.j1_30,
        '31-60 jours':     c.j31_60,
        '61-90 jours':     c.j61_90,
        '90+ jours':       c.j90Plus,
      }));
      rows.push({
        'Client':      'TOTAL',
        'Total impayé': this.balanceAgee.totalImpaye,
        'Courant':      this.balanceAgee.totalCourant,
        '1-30 jours':   this.balanceAgee.totalJ1_30,
        '31-60 jours':  this.balanceAgee.totalJ31_60,
        '61-90 jours':  this.balanceAgee.totalJ61_90,
        '90+ jours':    this.balanceAgee.totalJ90Plus,
      });
      const ws = XLSX.utils.json_to_sheet(rows);
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, ws, 'Balance Âgée');
      XLSX.writeFile(wb, `balance-agee-${new Date().toISOString().slice(0,10)}.xlsx`);

    } else if (this.tab === 'performance' && this.performance) {
      const clients = this.performance.topClients.map((c, i) => ({
        'Rang':            i + 1,
        'Client':          c.nomClient,
        'Nb ventes':       c.nombreVentes,
        'CA HT (MAD)':     c.montantTotalHT,
        'CA TTC (MAD)':    c.montantTotal,
        'Payé (MAD)':      c.montantPaye,
        'Impayé (MAD)':    c.montantImpaye,
        'Panier moyen':    c.panierMoyen,
      }));
      const produits = this.performance.topProduits.map((p, i) => ({
        'Rang':            i + 1,
        'Produit':         p.nomProduit,
        'Référence':       p.reference,
        'Qté vendue':      p.quantiteVendue,
        'CA HT (MAD)':     p.montantHT,
        '% du CA':         p.pourcentageCA,
      }));
      const wb = XLSX.utils.book_new();
      XLSX.utils.book_append_sheet(wb, XLSX.utils.json_to_sheet(clients),  'Top Clients');
      XLSX.utils.book_append_sheet(wb, XLSX.utils.json_to_sheet(produits), 'Top Produits');
      XLSX.writeFile(wb, `performance-${this.annee}.xlsx`);
    }
  }

  // ── PDF download ──────────────────────────────────────────────
  exportingPdf = false;

  canExportPdf(): boolean { return this.tab === 'pl' || this.tab === 'tva'; }

  async exportPdf(): Promise<void> {
    if (!this.canExportPdf()) return;
    this.exportingPdf = true;
    try {
      const type = this.tab as 'pl' | 'tva';
      const bytes = await this.api.rapportPdf(type, this.annee);
      const blob = new Blob([bytes], { type: 'application/pdf' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `rapport-${type}-${this.annee}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally { this.exportingPdf = false; }
  }

  resultClass(val: number): string {
    if (val > 0) return 'pos';
    if (val < 0) return 'neg';
    return '';
  }

  sign(val: number): string { return val >= 0 ? '+' : ''; }

  maxAbsResultat(): number {
    if (!this.plReport) return 1;
    return Math.max(1, ...this.plReport.mois.map(m => Math.abs(m.resultatNet)));
  }

  barWidth(val: number): number {
    const max = this.maxAbsResultat();
    return Math.round((Math.abs(val) / max) * 100);
  }
}
