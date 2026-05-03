import { Component, OnInit } from '@angular/core';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { DashboardStats, Vente } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass, getPayStatus, statusInfo, venteStatus } from '../../core/utils/format';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TopbarComponent, NgClass],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  stats: DashboardStats | null = null;
  recent: Vente[] = [];
  loading = true;

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;
  getPayStatus = getPayStatus;
  statusInfo = statusInfo;
  venteStatus = venteStatus;

  constructor(private api: ApiService) {}

  async ngOnInit(): Promise<void> {
    try {
      const [s, v] = await Promise.all([
        this.api.dashboardStats().catch(() => null),
        this.api.ventesList().catch(() => []),
      ]);
      this.stats = s;
      this.recent = v.slice(0, 5);
    } finally {
      this.loading = false;
    }
  }
}
