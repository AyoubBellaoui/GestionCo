import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { NotificationService } from '../../core/services/notification.service';
import { AppNotification, NotificationSummary } from '../../core/models';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.component.html',
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  summary: NotificationSummary = { unreadCount: 0, recent: [] };
  open = false;
  private _sub?: Subscription;

  constructor(public notifSvc: NotificationService, private router: Router) {}

  ngOnInit(): void {
    this.notifSvc.startPolling();
    this._sub = this.notifSvc.summary$.subscribe(s => this.summary = s);
  }

  ngOnDestroy(): void { this._sub?.unsubscribe(); }

  @HostListener('document:keydown.escape')
  onEscape(): void { this.open = false; }

  toggle(): void { this.open = !this.open; }

  typeIcon(type: AppNotification['type']): string {
    switch (type) {
      case 'Success': return '✅';
      case 'Warning': return '⚠️';
      case 'Danger': return '🔴';
      default: return 'ℹ️';
    }
  }

  typeCls(type: AppNotification['type']): string {
    switch (type) {
      case 'Success': return 'notif-success';
      case 'Warning': return 'notif-warning';
      case 'Danger': return 'notif-danger';
      default: return 'notif-info';
    }
  }

  async onNotifClick(n: AppNotification): Promise<void> {
    if (!n.isRead) await this.notifSvc.markRead(n.id);
    if (n.lienUrl) { this.open = false; this.router.navigate([n.lienUrl]); }
  }

  async markAll(): Promise<void> { await this.notifSvc.markAllRead(); }

  async deleteNotif(e: Event, id: number): Promise<void> {
    e.stopPropagation();
    await this.notifSvc.deleteNotification(id);
  }

  async clearRead(): Promise<void> { await this.notifSvc.deleteAllRead(); }

  get hasRead(): boolean { return this.summary.recent.some(n => n.isRead); }
}
