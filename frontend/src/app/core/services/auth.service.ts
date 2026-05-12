import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { User } from '../models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private userSubject = new BehaviorSubject<User | null>(null);
  private loadingSubject = new BehaviorSubject<boolean>(true);

  user$ = this.userSubject.asObservable();
  loading$ = this.loadingSubject.asObservable();

  get user(): User | null { return this.userSubject.value; }
  get loading(): boolean { return this.loadingSubject.value; }
  get isAdmin(): boolean { return this.userSubject.value?.role === 'Admin'; }
  get isGestionnaire(): boolean { return this.userSubject.value?.role === 'Gestionnaire'; }

  constructor() { this.init(); }

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

  setAuth(token: string, user: User): void {
    localStorage.setItem('gc_token', token);
    localStorage.setItem('gc_user', JSON.stringify(user));
    this.userSubject.next(user);
  }

  clearAuth(): void {
    localStorage.removeItem('gc_token');
    localStorage.removeItem('gc_user');
    this.userSubject.next(null);
  }

  logout(): void {
    this.clearAuth();
    window.location.href = '/login';
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
