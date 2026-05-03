import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toastsSubject = new BehaviorSubject<Toast[]>([]);
  toasts$ = this.toastsSubject.asObservable();

  notify(message: string, type: ToastType = 'info'): void {
    const id = Date.now() + Math.random();
    const toasts = [...this.toastsSubject.value, { id, message, type }];
    this.toastsSubject.next(toasts);
    setTimeout(() => {
      this.toastsSubject.next(this.toastsSubject.value.filter(t => t.id !== id));
    }, 3500);
  }

  iconFor(type: ToastType): string {
    return type === 'success' ? '✓' : type === 'error' ? '✕' : type === 'warning' ? '⚠' : 'ℹ';
  }
}
