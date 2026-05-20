import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { AuditLog } from '../../core/models';
import { getInitials, getAvatarClass } from '../../core/utils/format';

interface LogStats { total: number; sensitifs: number; creations: number; modifications: number; suppressions: number; }

@Component({
  selector: 'app-journal-audit',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './journal-audit.component.html',
})
export class JournalAuditComponent implements OnInit {
  logs: AuditLog[] = [];
  totalCount = 0;
  loading = true;
  page = 1;
  pageSize = 20;

  search = '';
  actionFilter = '';
  sensibleOnly = false;
  dateDebut = '';
  dateFin = '';

  stats: LogStats = { total: 0, sensitifs: 0, creations: 0, modifications: 0, suppressions: 0 };
  expandedId: number | null = null;

  private searchTimer: any;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  readonly entityIcons: Record<string, string> = {
    clients: '👥', ventes: '💰', factures: '🧾', achats: '📦', charges: '💸',
    devis: '📋', produits: '🏷️', fournisseurs: '🏭', paiements: '💳',
    utilisateurs: '🔐', categories: '📁',
  };

  readonly actionConfig: Record<string, { icon: string; cls: string; label: string }> = {
    Create:    { icon: '➕', cls: 'good',    label: 'Création' },
    Update:    { icon: '✏️', cls: 'medium',  label: 'Modification' },
    Delete:    { icon: '🗑️', cls: 'low',     label: 'Suppression' },
    Login:     { icon: '🔑', cls: 'pending', label: 'Connexion' },
    Logout:    { icon: '🚪', cls: 'neutral', label: 'Déconnexion' },
    Sensitive: { icon: '⚠️', cls: 'low',     label: 'Sensible' },
    Export:    { icon: '📤', cls: 'pending', label: 'Export' },
  };

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    await Promise.all([this.load(), this.loadStats()]);
  }

  async load(): Promise<void> {
    this.loading = true;
    try {
      const result = await this.api.auditListPaged({
        page: this.page, pageSize: this.pageSize,
        search: this.search || undefined,
        action: this.actionFilter || undefined,
        sensibleOnly: this.sensibleOnly || undefined,
      });
      this.logs = result.items;
      this.totalCount = result.totalCount;
    } finally { this.loading = false; }
  }

  async loadStats(): Promise<void> {
    try { this.stats = await this.api.auditStats(); } catch { }
  }

  onSearchChange(): void {
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => { this.page = 1; this.load(); }, 300);
  }

  onFilterChange(): void { this.page = 1; this.load(); }
  onPage(p: number): void { this.page = p; this.load(); }
  onPageSize(ps: number): void { this.pageSize = ps; this.page = 1; this.load(); }

  toggleExpand(id: number): void {
    this.expandedId = this.expandedId === id ? null : id;
  }

  actionStyle(action: string): { icon: string; cls: string; label: string } {
    return this.actionConfig[action] ?? { icon: '•', cls: 'pending', label: action };
  }

  entityIcon(entite: string): string {
    return this.entityIcons[entite?.toLowerCase()] ?? '📄';
  }

  formatDateTime(iso: string): { date: string; time: string } {
    const d = new Date(iso);
    return {
      date: d.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' }),
      time: d.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit', second: '2-digit' }),
    };
  }

  parseJson(raw: string | undefined): Record<string, any> | null {
    if (!raw) return null;
    try { return JSON.parse(raw); } catch { return null; }
  }

  jsonEntries(obj: Record<string, any> | null): { key: string; value: string }[] {
    if (!obj) return [];
    return Object.entries(obj).map(([key, value]) => ({ key, value: String(value ?? '—') }));
  }

  resetFilters(): void {
    this.search = ''; this.actionFilter = ''; this.sensibleOnly = false;
    this.dateDebut = ''; this.dateFin = ''; this.page = 1;
    this.load();
  }
}
