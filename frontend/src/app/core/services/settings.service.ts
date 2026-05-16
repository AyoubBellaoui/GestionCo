import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { AppSettings } from '../models';

const STORAGE_KEY = 'gestionco_settings';

const DEFAULT_SETTINGS: AppSettings = {
  devise: 'MAD',
  formatDate: 'dd/MM/yyyy',
  entreprise: {
    raisonSociale: 'GestionCo. SARL',
    adresse: '12 Boulevard Zerktouni, Casablanca 20250, Maroc',
    telephone: '+212 5 22 45 67 89',
    email: 'contact@gestionco.ma',
    ice: '002147896320087',
    rc: '458923 · Casablanca',
    if: '45782136',
    patente: '31458962',
    cnss: '7896541',
    capital: '100 000 MAD',
    rib: '007 640 0001234567890123 45',
    banque: 'Attijariwafa Bank',
    swift: 'BCMAMAMC',
    logo: '',
  },
  notifs: {
    nouvelleVente: true, stockFaible: true, factureImpayee: true,
    nouveauClient: false, emailDaily: false, emailWeekly: true,
  },
  facturation: {
    prefixeFacture: 'FAC', prefixeVente: 'VNT', prefixeAchat: 'ACH',
    prefixeProduit: 'PRD', tvaParDefaut: 20, delaiPaiement: 30,
  },
};

function loadFromStorage(): AppSettings {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved) {
      const parsed = JSON.parse(saved);
      return {
        ...DEFAULT_SETTINGS, ...parsed,
        entreprise: { ...DEFAULT_SETTINGS.entreprise, ...(parsed.entreprise || {}) },
        notifs: { ...DEFAULT_SETTINGS.notifs, ...(parsed.notifs || {}) },
        facturation: { ...DEFAULT_SETTINGS.facturation, ...(parsed.facturation || {}) },
      };
    }
  } catch { /* ignore */ }
  return DEFAULT_SETTINGS;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private settingsSubject = new BehaviorSubject<AppSettings>(loadFromStorage());
  settings$ = this.settingsSubject.asObservable();

  get settings(): AppSettings { return this.settingsSubject.value; }

  constructor() {
    this.settings$.subscribe(s => {
      try { localStorage.setItem(STORAGE_KEY, JSON.stringify(s)); } catch { /* ignore */ }
    });
    window.addEventListener('storage', (e) => {
      if (e.key === STORAGE_KEY && e.newValue) {
        try { this.settingsSubject.next(JSON.parse(e.newValue)); } catch { /* ignore */ }
      }
    });
  }

  setDraft<K extends keyof AppSettings>(key: K, value: AppSettings[K]): void {
    this.settingsSubject.next({ ...this.settings, [key]: value });
  }

  setDraftEntreprise(patch: Partial<AppSettings['entreprise']>): void {
    this.settingsSubject.next({ ...this.settings, entreprise: { ...this.settings.entreprise, ...patch } });
  }

  setDraftNotifs(patch: Partial<AppSettings['notifs']>): void {
    this.settingsSubject.next({ ...this.settings, notifs: { ...this.settings.notifs, ...patch } });
  }

  setDraftFacturation(patch: Partial<AppSettings['facturation']>): void {
    this.settingsSubject.next({ ...this.settings, facturation: { ...this.settings.facturation, ...patch } });
  }

  resetSettings(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.settingsSubject.next({ ...DEFAULT_SETTINGS });
  }

  formatMoney(amount: number, showCurrency = true): string {
    if (amount === null || amount === undefined || isNaN(amount)) return '—';
    const formatted = amount.toLocaleString('fr-FR', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    return showCurrency ? `${formatted} ${this.settings.devise}` : formatted;
  }

  formatDateValue(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    if (isNaN(d.getTime())) return '—';
    const dd = String(d.getDate()).padStart(2, '0');
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const yyyy = d.getFullYear();
    switch (this.settings.formatDate) {
      case 'MM/dd/yyyy': return `${mm}/${dd}/${yyyy}`;
      case 'yyyy-MM-dd': return `${yyyy}-${mm}-${dd}`;
      default:           return `${dd}/${mm}/${yyyy}`;
    }
  }
}
