import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { User } from '../models';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiService);
  private userSubject = new BehaviorSubject<User | null>(null);
  private loadingSubject = new BehaviorSubject<boolean>(true);

  user$ = this.userSubject.asObservable();
  loading$ = this.loadingSubject.asObservable();

  get user(): User | null { return this.userSubject.value; }
  get loading(): boolean { return this.loadingSubject.value; }
  get isAdmin(): boolean { return this.userSubject.value?.role === 'Admin'; }
  get isGestionnaire(): boolean { return this.userSubject.value?.role === 'Gestionnaire'; }
  get isAdminOrManager(): boolean { return this.isAdmin || this.isGestionnaire; }

  constructor() {
    this.init();
    window.addEventListener('storage', e => this.onOtherTabChange(e));
  }

  private init(): void {
    const stored = localStorage.getItem('gc_user');
    const token = localStorage.getItem('gc_token');
    if (stored && token) {
      try { this.userSubject.next(JSON.parse(stored)); } catch {
        localStorage.removeItem('gc_user');
      }
    }
    this.loadingSubject.next(false);
  }

  // One session per browser: follow a login / logout done in another tab
  // (a token refresh in another tab keeps the same user and changes nothing here)
  private onOtherTabChange(e: StorageEvent): void {
    if (e.key !== null && e.key !== 'gc_token' && e.key !== 'gc_user') return;
    let other: User | null = null;
    try {
      other = localStorage.getItem('gc_token') ? JSON.parse(localStorage.getItem('gc_user') || 'null') : null;
    } catch { other = null; }

    const current = this.userSubject.value;
    if (!other) {
      if (current) {
        this.userSubject.next(null);
        window.location.href = '/login';
      }
    } else if (!current || other.id !== current.id) {
      window.location.href = '/';
    }
  }

  setAuth(token: string, refreshToken: string, user: User): void {
    localStorage.setItem('gc_token', token);
    localStorage.setItem('gc_refresh', refreshToken);
    localStorage.setItem('gc_user', JSON.stringify(user));
    this.userSubject.next(user);
  }

  updateTokens(token: string, refreshToken: string): void {
    localStorage.setItem('gc_token', token);
    localStorage.setItem('gc_refresh', refreshToken);
  }

  clearAuth(): void {
    localStorage.removeItem('gc_token');
    localStorage.removeItem('gc_refresh');
    localStorage.removeItem('gc_user');
    this.userSubject.next(null);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('gc_refresh');
  }

  logout(): void {
    this.api.authLogout().catch(() => {}).finally(() => {
      this.clearAuth();
      window.location.href = '/login';
    });
  }

  updateUser(patch: Partial<User>): void {
    const current = this.userSubject.value;
    if (!current) return;
    const updated = { ...current, ...patch };
    localStorage.setItem('gc_user', JSON.stringify(updated));
    this.userSubject.next(updated);
  }

  getToken(): string | null {
    return localStorage.getItem('gc_token');
  }
}
