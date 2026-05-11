import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, interval, Subscription } from 'rxjs';
import { AppNotification, NotificationSummary } from '../models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private _summary$ = new BehaviorSubject<NotificationSummary>({ unreadCount: 0, recent: [] });
  private _polling$?: Subscription;

  readonly summary$ = this._summary$.asObservable();

  get unreadCount(): number { return this._summary$.value.unreadCount; }
  get notifications(): AppNotification[] { return this._summary$.value.recent; }

  constructor(private api: ApiService) {}

  startPolling(): void {
    if (this._polling$) return;
    this.refresh();
    this._polling$ = interval(30_000).subscribe(() => this.refresh());
  }

  stopPolling(): void {
    this._polling$?.unsubscribe();
    this._polling$ = undefined;
  }

  async refresh(): Promise<void> {
    try {
      const summary = await this.api.notificationsSummary(30);
      this._summary$.next(summary);
    } catch {
      // silent fail — network may be unavailable
    }
  }

  async markRead(id: number): Promise<void> {
    await this.api.notificationMarkRead(id);
    const curr = this._summary$.value;
    this._summary$.next({
      unreadCount: Math.max(0, curr.unreadCount - (curr.recent.find(n => n.id === id)?.isRead ? 0 : 1)),
      recent: curr.recent.map(n => n.id === id ? { ...n, isRead: true } : n)
    });
  }

  async markAllRead(): Promise<void> {
    await this.api.notificationMarkAllRead();
    const curr = this._summary$.value;
    this._summary$.next({
      unreadCount: 0,
      recent: curr.recent.map(n => ({ ...n, isRead: true }))
    });
  }

  async deleteNotification(id: number): Promise<void> {
    await this.api.notificationDelete(id);
    const curr = this._summary$.value;
    const removed = curr.recent.find(n => n.id === id);
    this._summary$.next({
      unreadCount: removed && !removed.isRead ? Math.max(0, curr.unreadCount - 1) : curr.unreadCount,
      recent: curr.recent.filter(n => n.id !== id)
    });
  }

  async deleteAllRead(): Promise<void> {
    await this.api.notificationDeleteAllRead();
    const curr = this._summary$.value;
    this._summary$.next({
      unreadCount: curr.unreadCount,
      recent: curr.recent.filter(n => !n.isRead)
    });
  }

  ngOnDestroy(): void { this.stopPolling(); }
}
