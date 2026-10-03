import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

const INACTIVE_MS  = 60 * 60 * 1000; // 60 minutes
const WARNING_MS   = 30 * 1000;       // warn 30 s before timeout
const ACTIVITY_KEY = 'gc_last_activity'; // last activity, shared by all tabs
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'wheel', 'touchstart', 'click', 'scroll'] as const;

@Injectable({ providedIn: 'root' })
export class InactivityService {
  private idleTimer?: ReturnType<typeof setTimeout>;
  private warnTimer?: ReturnType<typeof setTimeout>;
  private running = false;
  private lastMark = 0;
  private readonly onActivity = () => this.markActivity();

  constructor(
    private auth: AuthService,
    private router: Router,
    private toast: ToastService,
  ) {}

  start(): void {
    if (this.running) return;
    this.running = true;
    ACTIVITY_EVENTS.forEach(e =>
      window.addEventListener(e, this.onActivity, { passive: true, capture: true })
    );
    this.markActivity(true);
  }

  stop(): void {
    if (!this.running) return;
    this.running = false;
    ACTIVITY_EVENTS.forEach(e =>
      window.removeEventListener(e, this.onActivity, { capture: true } as any)
    );
    clearTimeout(this.idleTimer);
    clearTimeout(this.warnTimer);
  }

  // Record this tab's activity for every tab (at most once every 5 s)
  private markActivity(force = false): void {
    const now = Date.now();
    if (!force && now - this.lastMark < 5000) return;
    this.lastMark = now;
    localStorage.setItem(ACTIVITY_KEY, String(now));
    this.schedule();
  }

  // Idle time across all tabs: a tab left open but unused never logs out the tab in use
  private idleFor(): number {
    const last = Math.max(Number(localStorage.getItem(ACTIVITY_KEY)) || 0, this.lastMark);
    return Date.now() - last;
  }

  private schedule(): void {
    clearTimeout(this.idleTimer);
    clearTimeout(this.warnTimer);
    if (!this.running) return;

    const remaining = INACTIVE_MS - this.idleFor();
    if (remaining <= 0) {
      this.stop();
      this.auth.logout();
      return;
    }

    // Warning 30 s before logout
    if (remaining > WARNING_MS) {
      this.warnTimer = setTimeout(() => {
        if (INACTIVE_MS - this.idleFor() <= WARNING_MS) {
          this.toast.notify('Inactivité détectée — déconnexion dans 30 secondes', 'warning');
        }
      }, remaining - WARNING_MS);
    }

    // Re-check when the delay ends: activity in another tab postpones the logout
    this.idleTimer = setTimeout(() => this.schedule(), remaining);
  }
}
