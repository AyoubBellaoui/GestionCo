import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Client } from '../../core/models';
import { formatNum, getInitials, getAvatarClass } from '../../core/utils/format';

@Component({
  selector: 'app-clients',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './clients.component.html',
})
export class ClientsComponent implements OnInit {
  clients: Client[] = [];
  loading = true;
  search = '';
  typeFilter = '';

  formatNum = formatNum;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.clients = await this.api.clientsList().catch(() => []); }
    finally { this.loading = false; }
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

  async handleDelete(id: number): Promise<void> {
    if (!confirm('Supprimer ce client ?')) return;
    try { await this.api.clientDelete(id); this.toast.notify('Client supprimé', 'success'); this.load(); }
    catch { this.toast.notify('Erreur', 'error'); }
  }

  resetFilters(): void { this.search = ''; this.typeFilter = ''; }
}
