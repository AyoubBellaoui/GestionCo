import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { Fournisseur } from '../../core/models';
import { formatNum, getInitials, getAvatarClass } from '../../core/utils/format';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-fournisseurs',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './fournisseurs.component.html',
})
export class FournisseursComponent implements OnInit {
  fournisseurs: Fournisseur[] = [];
  totalCount = 0;
  loading = true;
  search = '';
  page = 1;
  pageSize = 15;
  statsData = { total: 0, achats: 0, commandes: 0, produits: 0 };
  private searchTimer: any;

  formatNum = formatNum;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, public auth: AuthService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats()]); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.fournisseursListPaged({ page: this.page, pageSize: this.pageSize, search: this.search || undefined });
      this.fournisseurs = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.fournisseursStats();
      this.statsData = { total: s.total, achats: s.achats, commandes: s.commandes, produits: s.produits };
    } catch (err) { console.error('loadStats fournisseurs error:', err); }
  }

  get filtered(): Fournisseur[] { return this.fournisseurs; }
  get stats() { return this.statsData; }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  importRunning = false;
  importProgress = '';
  importResult: { imported: number; failed: number; errors: { row: number; message: string }[] } | null = null;

  downloadFournisseurTemplate(): void {
    const ws = XLSX.utils.aoa_to_sheet([
      ['Nom', 'Telephone', 'Email', 'Adresse', 'Site Web', 'Personne Contact'],
      ['Fournisseur ABC', '+212600000000', 'contact@abc.ma', '123 Rue Industrielle, Casablanca', 'www.abc.ma', 'Mohammed'],
    ]);
    ws['!cols'] = [{ wch: 24 }, { wch: 16 }, { wch: 26 }, { wch: 34 }, { wch: 20 }, { wch: 20 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Fournisseurs');
    XLSX.writeFile(wb, 'modele-import-fournisseurs.xlsx');
  }

  triggerImportFournisseurs(): void {
    const input = document.getElementById('import-fournisseurs') as HTMLInputElement;
    input?.click();
  }

  async onImportFournisseursFile(event: Event): Promise<void> {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.importRunning = true;
    this.importProgress = 'Lecture du fichier…';
    try {
      const data = await file.arrayBuffer();
      const wb = XLSX.read(data);
      const rows: any[] = XLSX.utils.sheet_to_json(wb.Sheets[wb.SheetNames[0]]);
      if (rows.length === 0) { this.toast.notify('Fichier vide ou format incorrect', 'warning'); return; }
      if (rows.length > 500) { this.toast.notify('Maximum 500 lignes par import', 'warning'); return; }

      this.importProgress = `Envoi de ${rows.length} ligne(s)…`;

      const items = rows.map(r => ({
        nom: String(r['Nom'] || r['nom'] || ''),
        telephone: r['Telephone'] || r['Téléphone'] || r['telephone'] || '',
        email: r['Email'] || r['email'] || '',
        adresse: r['Adresse'] || r['adresse'] || '',
        siteWeb: r['Site Web'] || r['siteWeb'] || r['Site web'] || '',
        personneContact: r['Personne Contact'] || r['Contact'] || r['contact'] || '',
        isActive: true,
      }));

      const result = await this.api.fournisseurBulkImport(items);
      await this.load();
      this.importResult = result;
      const msg = `Import terminé : ${result.imported} fournisseur(s) créé(s)${result.failed > 0 ? ', ' + result.failed + ' erreur(s)' : ''}`;
      this.toast.notify(msg, result.imported > 0 ? 'success' : 'warning');
    } catch { this.toast.notify('Erreur lors de la lecture du fichier', 'error'); }
    finally { this.importRunning = false; this.importProgress = ''; (event.target as HTMLInputElement).value = ''; }
  }

  async handleDelete(id: number): Promise<void> {
    if (!confirm('Supprimer ce fournisseur ?')) return;
    try { await this.api.fournisseurDelete(id); this.toast.notify('Fournisseur supprimé', 'success'); this.load(); }
    catch { this.toast.notify('Erreur', 'error'); }
  }
}
