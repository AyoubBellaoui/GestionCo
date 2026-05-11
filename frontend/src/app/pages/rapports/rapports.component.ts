import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass, DecimalPipe } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { PLReport, TVAReport } from '../../core/models';
import { formatNum } from '../../core/utils/format';
import * as XLSX from 'xlsx';

type Tab = 'pl' | 'tva';

@Component({
  selector: 'app-rapports',
  standalone: true,
  imports: [FormsModule, NgClass, DecimalPipe, TopbarComponent],
  templateUrl: './rapports.component.html',
})
export class RapportsComponent implements OnInit {
  tab: Tab = 'pl';
  annee = new Date().getFullYear();
  annees: number[] = [];

  plReport: PLReport | null = null;
  tvaReport: TVAReport | null = null;
  loading = false;

  formatNum = formatNum;

  constructor(private api: ApiService) {
    const current = new Date().getFullYear();
    for (let y = current; y >= current - 4; y--) this.annees.push(y);
  }

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const [pl, tva] = await Promise.all([
        this.api.rapportPL(this.annee),
        this.api.rapportTVA(this.annee),
      ]);
      this.plReport = pl;
      this.tvaReport = tva;
    } finally { this.loading = false; }
  }

  async onAnneeChange(): Promise<void> { await this.load(); }

  // ── Excel export ──────────────────────────────────────────
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
    }
  }

  // ── PDF download ──────────────────────────────────────────
  exportingPdf = false;

  async exportPdf(): Promise<void> {
    this.exportingPdf = true;
    try {
      const type = this.tab;
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
