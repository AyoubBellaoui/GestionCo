import { Component, OnInit } from '@angular/core';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { FullDashboard } from '../../core/models';
import { formatNum } from '../../core/utils/format';

interface MonthBar  { label: string; ca: number; dep: number; caH: number; depH: number; }
interface DonutSeg  { dasharray: string; dashoffset: number; color: string; label: string; amount: number; pct: number; }

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TopbarComponent, NgClass],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  d: FullDashboard | null = null;
  loading = true;
  formatNum = formatNum;

  // Presentation-only (SVG math / bar heights computed from API data)
  months:    MonthBar[] = [];
  donutSegs: DonutSeg[] = [];

  readonly DONUT_CIRC = +(2 * Math.PI * 42).toFixed(2);
  readonly MINI_CIRC  = 125.66;

  // ── Presentation getters ────────────────────────────────────────────────────
  get miniRingDash(): string {
    const r = this.d?.tauxEncaissement ?? 0;
    return `${+((r / 100) * this.MINI_CIRC).toFixed(2)} ${this.MINI_CIRC}`;
  }
  get ringColor(): string {
    const r = this.d?.tauxEncaissement ?? 0;
    return r >= 75 ? 'var(--success)' : r >= 40 ? 'var(--warning)' : 'var(--danger)';
  }
  get margeBarPct(): number { return Math.min(Math.abs(this.d?.margeRate ?? 0), 100); }
  get currentMonth(): string { return new Date().toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' }); }

  get revenuTrendCls(): string  { const t = this.d?.trendRevenu ?? 0;   return t > 0 ? '' : t < 0 ? 'down' : 'neutral'; }
  get depensesTrendCls(): string{ const t = this.d?.trendDepenses ?? 0; return t > 0 ? 'down' : t < 0 ? '' : 'neutral'; }
  get revenuTrendText(): string {
    const t = this.d?.trendRevenu ?? 0;
    return t === 0 ? '— Stable vs mois dernier' : `${t > 0 ? '▲' : '▼'} ${Math.abs(t)}% vs mois dernier`;
  }
  get depensesTrendText(): string {
    const t = this.d?.trendDepenses ?? 0;
    return t === 0 ? '— Stable vs mois dernier' : `${t > 0 ? '▲' : '▼'} ${Math.abs(t)}% vs mois dernier`;
  }

  // shortcuts so HTML stays clean
  get stats() { return this.d; }
  get revenus()          { return this.d?.caDuMois        ?? 0; }
  get depenses()         { return this.d?.depensesDuMois   ?? 0; }
  get resultatNet()      { return this.d?.resultatNet      ?? 0; }
  get margeRate()        { return this.d?.margeRate        ?? 0; }
  get achatsDuMois()     { return this.d?.achatsDuMois     ?? 0; }
  get chargesDuMois()    { return this.d?.chargesDuMois    ?? 0; }
  get tauxEncaissement()    { return this.d?.tauxEncaissement    ?? 0; }
  get panierMoyen()         { return this.d?.panierMoyen         ?? 0; }
  get totalQteVendue()      { return this.d?.totalQteVendue      ?? 0; }
  get tauxFidelite()        { return this.d?.tauxFidelite        ?? 0; }
  get montantImpayeDebit()  { return this.d?.montantImpayeDebit  ?? 0; }
  get montantImpayeCredit() { return this.d?.montantImpaye       ?? 0; }
  get nombreAchatsImpayes()   { return this.d?.nombreAchatsImpayes   ?? 0; }
  get nombreChargesImpayees() { return this.d?.nombreChargesImpayees ?? 0; }
  get nombreFacturesImpayees(){ return this.d?.nombreFacturesImpayees ?? 0; }

  // Remapped for HTML template compatibility
  private readonly _avCls = ['avatar-1','avatar-2','avatar-3','avatar-4','avatar-5'];

  get topClients(): { nom: string; initiales: string; total: number; pct: number; cls: string }[] {
    const list = this.d?.topClients ?? [];
    const max  = Math.max(...list.map(c => c.totalDepense), 1);
    return list.map((c, i) => ({
      nom:      c.nomClient,
      initiales: c.initiales || c.nomClient.substring(0, 2).toUpperCase(),
      total:    c.totalDepense,
      pct:      Math.round((c.totalDepense / max) * 100),
      cls:      this._avCls[i % this._avCls.length],
    }));
  }

  get topProduits(): { nom: string; ref: string; ca: number; qte: number; pct: number }[] {
    const list = this.d?.topProduits ?? [];
    const max  = Math.max(...list.map(p => p.montantTotal), 1);
    return list.map(p => ({
      nom: p.nomProduit,
      ref: String(p.produitId),
      ca:  p.montantTotal,
      qte: p.quantiteVendue,
      pct: Math.round((p.montantTotal / max) * 100),
    }));
  }

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try {
      this.d = await this.api.dashboardFull();
      this._buildCharts();
    } finally {
      this.loading = false;
    }
  }

  private _buildCharts(): void {
    if (!this.d) return;

    // Bar heights (pure presentation normalisation)
    const maxVal = Math.max(...this.d.last6Months.map(b => Math.max(b.ca, b.dep)), 1);
    this.months = this.d.last6Months.map(b => ({
      ...b,
      caH:  Math.round((b.ca  / maxVal) * 100),
      depH: Math.round((b.dep / maxVal) * 100),
    }));

    // Donut SVG segments
    const total = this.d.donutItems.reduce((s, i) => s + i.amount, 0);
    if (total > 0) {
      let off = 0;
      this.donutSegs = this.d.donutItems.map(item => {
        const len = (item.amount / total) * this.DONUT_CIRC;
        const seg: DonutSeg = {
          dasharray:  `${+len.toFixed(2)} ${this.DONUT_CIRC}`,
          dashoffset: +(-off).toFixed(2),
          color: item.color, label: item.label,
          amount: item.amount,
          pct: Math.round((item.amount / total) * 100),
        };
        off += len;
        return seg;
      });
    }
  }
}
