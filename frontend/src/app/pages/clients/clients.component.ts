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
  loading = true;
  search = '';
  typeFilter = '';
  page = 1;
  pageSize = 15;

  formatNum = formatNum;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, public auth: AuthService) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.clients = await this.api.clientsList().catch(() => []); }
    finally { this.loading = false; }
  }

  get paged(): Client[] {
    return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize);
  }

  get filtered(): Client[] {
    return this.clients.filter(c => {
      if (this.search && !c.nomClient.toLowerCase().includes(this.search.toLowerCase()) &&
        !(c.email || '').toLowerCase().includes(this.search.toLowerCase()) &&
        !(c.ice || '').includes(this.search)) return false;
      if (this.typeFilter && c.type !== this.typeFilter) return false;
      return true;
    });
  }

  get stats() {
    return {
      total: this.clients.length,
      actifs: this.clients.filter(c => c.isActive).length,
      ca: this.clients.reduce((s, c) => s + (c.totalDepense || 0), 0),
      impayes: this.clients.reduce((s, c) => s + (c.totalImpaye || 0), 0),
    };
  }

  importRunning = false;
  importProgress = '';

  downloadClientTemplate(): void {
    const ws = XLSX.utils.aoa_to_sheet([
      ['Nom Client', 'Type', 'Telephone', 'Email', 'Ville', 'ICE', 'RC', 'IF', 'Personne Contact'],
      ['Société ABC', 'Entreprise', '+212600000000', 'contact@abc.ma', 'Casablanca', '002000000000000', 'RC123', 'IF456', 'Mohammed'],
      ['Jean Dupont', 'Particulier', '+212611111111', 'jean@mail.com', 'Rabat', '', '', '', ''],
    ]);
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
      let ok = 0; let errors = 0;
      for (let i = 0; i < rows.length; i++) {
        const r = rows[i];
        this.importProgress = `Import ${i + 1}/${rows.length}…`;
        const nom = r['Nom Client'] || r['Nom'] || r['nom'];
        if (!nom) { errors++; continue; }
        try {
          await this.api.clientCreate({
            nomClient: String(nom),
            type: r['Type'] || r['type'] || 'Particulier',
            telephone: r['Telephone'] || r['Téléphone'] || r['telephone'] || '',
            email: r['Email'] || r['email'] || '',
            ville: r['Ville'] || r['ville'] || '',
            ice: r['ICE'] || r['ice'] || '',
            rc: r['RC'] || r['rc'] || '',
            if: r['IF'] || r['if'] || '',
            personneContact: r['Personne Contact'] || r['Contact'] || '',
          });
          ok++;
        } catch { errors++; }
      }
      await this.load();
      this.toast.notify(`Import terminé : ${ok} client(s) créé(s)${errors > 0 ? ', ' + errors + ' erreur(s)' : ''}`, ok > 0 ? 'success' : 'warning');
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

  resetFilters(): void { this.search = ''; this.typeFilter = ''; this.page = 1; }
}
