import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly KEY = 'gc_theme';
  private _dark = false;

  get isDark(): boolean { return this._dark; }

  constructor() {
    const saved = localStorage.getItem(this.KEY);
    this._dark = saved ? saved === 'dark' : window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.apply();
  }

  toggle(): void {
    document.body.classList.add('theme-transitioning');
    this._dark = !this._dark;
    localStorage.setItem(this.KEY, this._dark ? 'dark' : 'light');
    this.apply();
    setTimeout(() => document.body.classList.remove('theme-transitioning'), 300);
  }

  private apply(): void {
    document.documentElement.setAttribute('data-theme', this._dark ? 'dark' : 'light');
  }
}
