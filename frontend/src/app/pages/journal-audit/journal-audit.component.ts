import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { ApiService } from '../../core/services/api.service';
import { AuditLog } from '../../core/models';
import { formatDate, getInitials, getAvatarClass } from '../../core/utils/format';

@Component({
  selector: 'app-journal-audit',
  standalone: true,
  imports: [TopbarComponent, PaginationComponent, FormsModule, NgClass],
  templateUrl: './journal-audit.component.html',
})
export class JournalAuditComponent implements OnInit {
  logs: AuditLog[] = [];
  loading = true;
  search = '';
  actionFilter = '';
  sensibleOnly = false;
  page = 1;
  pageSize = 25;

  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try { this.logs = await this.api.auditList().catch(() => []); }
    finally { this.loading = false; }
  }

  get paged(): AuditLog[] {
    return this.filtered.slice((this.page - 1) * this.pageSize, this.page * this.pageSize);
  }

  get filtered(): AuditLog[] {
    return this.logs.filter(l => {
      if (this.search &&
        !l.nomUtilisateur.toLowerCase().includes(this.search.toLowerCase()) &&
        !l.actionLibelle.toLowerCase().includes(this.search.toLowerCase()) &&
        !l.entite.toLowerCase().includes(this.search.toLowerCase()) &&
        !l.description.toLowerCase().includes(this.search.toLowerCase())) return false;
      if (this.actionFilter && l.action !== this.actionFilter) return false;
      if (this.sensibleOnly && !l.estSensible) return false;
      return true;
    });
  }

  get uniqueActions(): { value: string; label: string }[] {
    return Array.from(new Set(this.logs.map(l => l.action)))
      .map(a => ({ value: a, label: this.logs.find(l => l.action === a)?.actionLibelle || a }));
  }

  actionStyle(action: string): { icon: string; cls: string } {
    const map: Record<string, { icon: string; cls: string }> = {
      'Create': { icon: '➕', cls: 'good' },
      'Update': { icon: '✏️', cls: 'medium' },
      'Delete': { icon: '🗑️', cls: 'low' },
      'Login': { icon: '🔑', cls: 'good' },
      'Logout': { icon: '🚪', cls: 'pending' },
      'Export': { icon: '📤', cls: 'pending' },
    };
    return map[action] || { icon: '•', cls: 'pending' };
  }

  getUpdateCount(): number { return this.logs.filter(l => l.action === 'Update').length; }
  getSensibleCount(): number { return this.logs.filter(l => l.estSensible).length; }
  getCreateCount(): number { return this.logs.filter(l => l.action === 'Create').length; }

  resetFilters(): void { this.search = ''; this.actionFilter = ''; this.sensibleOnly = false; this.page = 1; }
}
