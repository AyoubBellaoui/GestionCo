const STORAGE_KEY = 'gestionco_settings';

interface CachedSettings {
  devise: 'MAD' | 'EUR' | 'USD';
  formatDate: 'dd/MM/yyyy' | 'MM/dd/yyyy' | 'yyyy-MM-dd';
  timezone: string;
  heureFormat: '24h' | '12h';
}

const DEFAULT_SETTINGS: CachedSettings = {
  devise: 'MAD',
  formatDate: 'dd/MM/yyyy',
  timezone: 'Africa/Casablanca',
  heureFormat: '24h',
};

function getCurrentSettings(): CachedSettings {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return DEFAULT_SETTINGS;
    const parsed = JSON.parse(raw);
    return {
      devise: parsed.devise || DEFAULT_SETTINGS.devise,
      formatDate: parsed.formatDate || DEFAULT_SETTINGS.formatDate,
      timezone: parsed.timezone || DEFAULT_SETTINGS.timezone,
      heureFormat: parsed.heureFormat || DEFAULT_SETTINGS.heureFormat,
    };
  } catch { return DEFAULT_SETTINGS; }
}

export const formatNum = (n: number | undefined | null): string =>
  (n ?? 0).toLocaleString('fr-FR');

export const formatMAD = (n: number | undefined | null): string => {
  const { devise } = getCurrentSettings();
  return `${formatNum(n)} ${devise}`;
};

export const formatMoney = formatMAD;
export const getCurrentDevise = (): string => getCurrentSettings().devise;

export const formatDate = (iso: string | undefined): { main: string; time: string } => {
  if (!iso) return { main: '—', time: '' };
  try {
    const d = new Date(iso);
    if (isNaN(d.getTime())) return { main: iso, time: '' };
    const { formatDate: fmt, timezone, heureFormat } = getCurrentSettings();

    const dateParts = new Intl.DateTimeFormat('en-CA', {
      timeZone: timezone, year: 'numeric', month: '2-digit', day: '2-digit',
    }).formatToParts(d);
    const get = (t: string) => dateParts.find(p => p.type === t)?.value ?? '';
    const dd = get('day'); const mm = get('month'); const yyyy = get('year');

    let main: string;
    if (fmt === 'MM/dd/yyyy') {
      main = `${mm}/${dd}/${yyyy}`;
    } else if (fmt === 'yyyy-MM-dd') {
      main = `${yyyy}-${mm}-${dd}`;
    } else {
      const months = ['jan', 'fév', 'mar', 'avr', 'mai', 'juin', 'juil', 'aoû', 'sep', 'oct', 'nov', 'déc'];
      main = `${parseInt(dd)} ${months[parseInt(mm) - 1]} ${yyyy}`;
    }

    const timeParts = new Intl.DateTimeFormat('en-GB', {
      timeZone: timezone, hour: '2-digit', minute: '2-digit', hour12: heureFormat === '12h',
    }).formatToParts(d);
    const getT = (t: string) => timeParts.find(p => p.type === t)?.value ?? '';
    let time: string;
    if (heureFormat === '12h') {
      const period = timeParts.find(p => p.type === 'dayPeriod')?.value ?? '';
      time = `${getT('hour')}:${getT('minute')} ${period}`;
    } else {
      time = `${getT('hour')}:${getT('minute')}`;
    }

    return { main, time };
  } catch { return { main: iso, time: '' }; }
};

export const getInitials = (name: string): string => {
  if (!name) return '—';
  const parts = name.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
};

export const getAvatarClass = (seed: string | number): string => {
  const s = typeof seed === 'string'
    ? seed.split('').reduce((a, c) => a + c.charCodeAt(0), 0)
    : seed;
  return `avatar-${(s % 6) + 1}`;
};

export const getPayStatus = (paye: number, total: number): { cls: string; pct: number } => {
  if (total === 0) return { cls: 'unpaid', pct: 0 };
  const pct = Math.max(2, Math.min(100, (paye / total) * 100));
  if (paye >= total) return { cls: 'paid', pct: 100 };
  if (paye > 0) return { cls: 'partial', pct };
  return { cls: 'unpaid', pct: 0 };
};

export const venteStatus = (statut: string, montantPaye: number): { label: string; cls: string } => {
  if (statut === 'EnAttente' && montantPaye > 0) return { label: 'Partiel', cls: 'partial' };
  return statusInfo(statut);
};

export const statusInfo = (statut: string): { label: string; cls: string } => {
  const m: Record<string, { label: string; cls: string }> = {
    'Paye':      { label: 'Payée',      cls: 'paid' },
    'Payee':     { label: 'Payée',      cls: 'paid' },
    'EnAttente': { label: 'En attente', cls: 'pending' },
    'Annulee':   { label: 'Annulée',    cls: 'cancelled' },
    'Annule':    { label: 'Annulé',     cls: 'cancelled' },
    'Recu':      { label: 'Reçu',       cls: 'paid' },
    'EnCommande':{ label: 'En commande',cls: 'pending' },
  };
  return m[statut] || { label: statut, cls: 'pending' };
};
