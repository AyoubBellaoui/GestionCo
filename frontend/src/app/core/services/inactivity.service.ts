import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { ToastService } from './toast.service';

const INACTIVE_MS  = 5 * 60 * 1000; // 5 minutes
const WARNING_MS   = 30 * 1000;      // warn 30 s before timeout
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'wheel', 'touchstart', 'click', 'scroll'] as const;

@Injectable({ providedIn: 'root' })
export class InactivityService {
  private idleTimer?: ReturnType<typeof setTimeout>;
  private warnTimer?: ReturnType<typeof setTimeout>;
  private running = false;
  private readonly onActivity = () => this.reset();

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
    this.schedule();
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

  private reset(): void {
    clearTimeout(this.idleTimer);
    clearTimeout(this.warnTimer);
    this.schedule();
  }

  private schedule(): void {
    // Warning 30 s before logout
    this.warnTimer = setTimeout(() => {
      this.toast.notify('Inactivité détectée — déconnexion dans 30 secondes', 'warning');
    }, INACTIVE_MS - WARNING_MS);

    // Actual logout
    this.idleTimer = setTimeout(() => {
      this.stop();
      this.auth.logout();
    }, INACTIVE_MS);
  }
}
