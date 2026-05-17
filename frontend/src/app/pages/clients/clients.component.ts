import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { Client } from '../../core/models';
import { formatNum, getInitials, getAvatarClass } from '../../core/utils/format';
import * as XLSX from 'xlsx';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './clients.component.html',
})
export class ClientsComponent implements OnInit {
  clients: Client[] = [];
  totalCount = 0;
  loading = true;
  search = '';
  typeFilter = '';
  page = 1;
  pageSize = 15;
  statsData = { total: 0, actifs: 0, ca: 0, impayes: 0 };
  private searchTimer: any;

  formatNum = formatNum;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, public auth: AuthService) {}

  async ngOnInit(): Promise<void> { await Promise.all([this.load(), this.loadStats()]); }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.clientsListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        type: this.typeFilter || undefined,
      });
      this.clients = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try {
      const s = await this.api.clientsStats();
      this.statsData = { total: s.total, actifs: s.actifs, ca: s.ca, impayes: s.impayes };
    } catch (err) { console.error('loadStats clients error:', err); }
  }

  get paged(): Client[] { return this.clients; }
  get filtered(): Client[] { return this.clients; }

  get stats() { return this.statsData; }

  onFilterChange(): void { this.page = 1; this.load(); }
  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  importRunning = false;
  importProgress = '';
  importResult: { imported: number; failed: number; errors: { row: number; message: string }[] } | null = null;

  downloadClientTemplate(): void {
    const ws = XLSX.utils.aoa_to_sheet([
      ['Nom Client', 'Type', 'Telephone', 'Email', 'Ville', 'Adresse', 'ICE', 'RC', 'IF', 'Personne Contact'],
      ['Société ABC', 'Entreprise', '+212600000000', 'contact@abc.ma', 'Casablanca', '123 Rue Hassan II', '002000000000000', 'RC123', 'IF456', 'Mohammed'],
      ['Jean Dupont', 'Particulier', '+212611111111', 'jean@mail.com', 'Rabat', '', '', '', '', ''],
    ]);
    ws['!cols'] = [{ wch: 22 }, { wch: 12 }, { wch: 16 }, { wch: 24 }, { wch: 14 }, { wch: 24 }, { wch: 16 }, { wch: 8 }, { wch: 8 }, { wch: 18 }];
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Clients');
    XLSX.writeFile(wb, 'modele-import-clients.xlsx');
  }

  triggerImportClients(): void {
    const input = document.getElementById('import-clients') as HTMLInputElement;
    input?.click();
  }

  async onImportClientsFile(event: Event): Promise<void> {
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
        nomClient: String(r['Nom Client'] || r['Nom'] || r['nom'] || ''),
        type: r['Type'] || r['type'] || 'Particulier',
        telephone: r['Telephone'] || r['Téléphone'] || r['telephone'] || '',
        email: r['Email'] || r['email'] || '',
        ville: r['Ville'] || r['ville'] || '',
        adresse: r['Adresse'] || r['adresse'] || '',
        iCE: r['ICE'] || r['ice'] || '',
        rC: r['RC'] || r['rc'] || '',
        iF: r['IF'] || r['if'] || '',
        personneContact: r['Personne Contact'] || r['Contact'] || r['contact'] || '',
        isActive: true,
        creerCompte: false,
      }));

      const result = await this.api.clientBulkImport(items);
      await this.load();
      this.importResult = result;
      const msg = `Import terminé : ${result.imported} client(s) créé(s)${result.failed > 0 ? ', ' + result.failed + ' erreur(s)' : ''}`;
      this.toast.notify(msg, result.imported > 0 ? 'success' : 'warning');
    } catch { this.toast.notify('Erreur lors de la lecture du fichier', 'error'); }
    finally { this.importRunning = false; this.importProgress = ''; (event.target as HTMLInputElement).value = ''; }
  }

  async handleDelete(id: number): Promise<void> {
    if (!confirm('Supprimer ce client ?')) return;
    try {
      await this.api.clientDelete(id);
      this.toast.notify('Client supprimé avec succès', 'success');
      this.load();
    } catch (err: any) {
      const msg = err?.error?.message || 'Erreur lors de la suppression';
      this.toast.notify(msg, 'error');
    }
  }

  resetFilters(): void { this.search = ''; this.typeFilter = ''; this.page = 1; this.load(); }
}
