import { Component, NgZone, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { SettingsService } from '../../core/services/settings.service';
import { UtilisateurDto, CreateUtilisateurPayload, UpdateUtilisateurPayload, SmtpSettings } from '../../core/models';
import { getAvatarClass } from '../../core/utils/format';

type TabKey = 'entreprise' | 'profil' | 'apparence' | 'securite' | 'facturation' | 'email' | 'comptes' | 'a-propos';

const TABS: { key: TabKey; label: string; icon: string; desc: string; adminOnly?: boolean }[] = [
  { key: 'entreprise',  label: 'Entreprise',  icon: '🏢', desc: 'Infos légales, ICE, RC, IF' },
  { key: 'profil',      label: 'Mon profil',  icon: '👤', desc: 'Informations personnelles' },
  { key: 'apparence',   label: 'Apparence',   icon: '🎨', desc: 'Date, heure & fuseau horaire' },
  { key: 'securite',    label: 'Sécurité',    icon: '🔒', desc: 'Mot de passe, sessions' },
  { key: 'facturation', label: 'Facturation', icon: '📋', desc: 'Numérotation, TVA, délais' },
  { key: 'email',       label: 'Email SMTP',  icon: '📧', desc: 'Serveur d\'envoi des emails', adminOnly: true },
  { key: 'comptes',     label: 'Comptes',     icon: '👥', desc: 'Gérer les accès utilisateurs', adminOnly: true },
  { key: 'a-propos',    label: 'À propos',    icon: 'ℹ️', desc: 'Version & infos système' },
];

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './settings.component.html',
})
export class SettingsComponent implements OnInit {
  tabs = TABS;
  activeTab: TabKey = 'entreprise';
  getAvatarClass = getAvatarClass;

  profil = { prenom: '', nom: '', telephone: '' };
  securite = { ancienMdp: '', nouveauMdp: '', confirmMdp: '' };
  securiteChanging = false;
  showPwd = { ancien: false, nouveau: false, confirm: false };
  factuLoading = false;

  smtp: SmtpSettings = { host: '', port: 587, username: '', password: '', fromName: '', fromAddress: '', enableSsl: true };
  smtpLoading = false;
  smtpSaving = false;
  smtpTesting = false;
  smtpTestEmail = '';
  showSmtpPassword = false;

  comptesList: UtilisateurDto[] = [];
  comptesLoading = false;
  comptesModalOpen = false;
  comptesModalMode: 'create' | 'edit' = 'create';
  comptesEditId: number | null = null;
  comptesSaving = false;
  compteForm = { prenom: '', nom: '', email: '', password: '', role: 'Gestionnaire', telephone: '', isActive: true };
  comptesSearch = '';
  comptesRoleFilter = '';

  constructor(
    public auth: AuthService,
    public settings: SettingsService,
    private api: ApiService,
    private toast: ToastService,
    private ngZone: NgZone,
  ) {}

  ngOnInit(): void {
    const u = this.auth.user;
    if (u) {
      this.profil.prenom = u.prenom || '';
      this.profil.nom = u.nom || '';
      this.profil.telephone = u.telephone || '';
    }
  }

  get visibleTabs() {
    return this.tabs.filter(t => !t.adminOnly || this.auth.user?.role === 'Admin');
  }

  get filteredComptes(): UtilisateurDto[] {
    return this.comptesList.filter(u => {
      if (this.comptesSearch && !u.nomComplet.toLowerCase().includes(this.comptesSearch.toLowerCase()) &&
        !u.email.toLowerCase().includes(this.comptesSearch.toLowerCase())) return false;
      if (this.comptesRoleFilter && u.role !== this.comptesRoleFilter) return false;
      return true;
    });
  }

  async setTab(key: TabKey): Promise<void> {
    this.activeTab = key;
    if (key === 'comptes') await this.loadComptes();
    if (key === 'facturation') await this.loadFacturationSettings();
    if (key === 'entreprise') await this.loadEntrepriseSettings();
    if (key === 'email') await this.loadSmtpSettings();
    if (key === 'securite') {
      this.securite = { ancienMdp: '', nouveauMdp: '', confirmMdp: '' };
      this.showPwd = { ancien: false, nouveau: false, confirm: false };
    }
  }

  async loadFacturationSettings(): Promise<void> {
    this.factuLoading = true;
    try {
      const data = await this.api.getFacturationSettings();
      this.settings.setDraftFacturation(data);
    } catch { /* use local defaults if backend unreachable */ }
    finally { this.factuLoading = false; }
  }

  async loadEntrepriseSettings(): Promise<void> {
    try {
      const data = await this.api.getEntrepriseSettings();
      this.settings.setDraftEntreprise({
        raisonSociale: data.raisonSociale ?? this.settings.settings.entreprise.raisonSociale,
        adresse: data.adresse ?? this.settings.settings.entreprise.adresse,
        telephone: data.telephone ?? this.settings.settings.entreprise.telephone,
        email: data.email ?? this.settings.settings.entreprise.email,
        ice: data.ice ?? this.settings.settings.entreprise.ice,
        rc: data.rc ?? this.settings.settings.entreprise.rc,
        if: data.if ?? this.settings.settings.entreprise.if,
        patente: data.patente ?? this.settings.settings.entreprise.patente,
        cnss: data.cnss ?? this.settings.settings.entreprise.cnss,
        capital: data.capital ?? this.settings.settings.entreprise.capital,
        rib: data.rib ?? this.settings.settings.entreprise.rib,
        banque: data.banque ?? this.settings.settings.entreprise.banque,
        swift: data.swift ?? this.settings.settings.entreprise.swift,
        logo: data.logo ?? this.settings.settings.entreprise.logo,
      });
    } catch { /* use local defaults if backend unreachable */ }
  }

  setIce(value: string): void {
    this.settings.setDraftEntreprise({ ice: value.replace(/\D/g, '').slice(0, 15) });
  }

  async handleApply(): Promise<void> {
    if (this.activeTab === 'facturation') {
      try {
        await this.api.updateFacturationSettings(this.settings.settings.facturation);
        this.toast.notify('Paramètres de facturation sauvegardés', 'success');
      } catch {
        this.toast.notify('Erreur lors de la sauvegarde', 'error');
      }
    } else if (this.activeTab === 'entreprise') {
      try {
        await this.api.updateEntrepriseSettings(this.settings.settings.entreprise as any);
        this.toast.notify('Informations entreprise sauvegardées', 'success');
      } catch {
        this.toast.notify('Erreur lors de la sauvegarde', 'error');
      }
    } else {
      this.toast.notify('Paramètres sauvegardés', 'success');
    }
  }

  async handleSaveProfil(): Promise<void> {
    if (!this.profil.prenom.trim() || !this.profil.nom.trim()) {
      this.toast.notify('Prénom et nom sont requis', 'warning'); return;
    }
    try {
      const updated = await this.api.updateProfile({
        prenom: this.profil.prenom.trim(),
        nom: this.profil.nom.trim(),
        telephone: this.profil.telephone.trim() || undefined,
      });
      const initiales = `${updated.prenom[0] || ''}${updated.nom[0] || ''}`.toUpperCase();
      this.auth.updateUser({ prenom: updated.prenom, nom: updated.nom, telephone: updated.telephone, initiales });
      this.toast.notify('Profil mis à jour', 'success');
    } catch {
      this.toast.notify('Erreur lors de la mise à jour', 'error');
    }
  }

  async handleChangePassword(): Promise<void> {
    if (!this.securite.ancienMdp || !this.securite.nouveauMdp) {
      this.toast.notify('Remplissez tous les champs', 'warning'); return;
    }
    if (this.securite.nouveauMdp !== this.securite.confirmMdp) {
      this.toast.notify('Les mots de passe ne correspondent pas', 'error'); return;
    }
    if (this.securite.nouveauMdp.length < 8) {
      this.toast.notify('Le mot de passe doit contenir au moins 8 caractères', 'warning'); return;
    }
    this.securiteChanging = true;
    try {
      await this.api.changePassword({
        currentPassword: this.securite.ancienMdp,
        newPassword: this.securite.nouveauMdp,
      });
      this.toast.notify('Mot de passe modifié avec succès', 'success');
      this.securite = { ancienMdp: '', nouveauMdp: '', confirmMdp: '' };
    } catch {
      this.toast.notify('Mot de passe actuel incorrect', 'error');
    } finally {
      this.securiteChanging = false;
    }
  }

  handleLogoUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      this.toast.notify('Veuillez sélectionner une image valide', 'warning'); return;
    }
    if (file.size > 2 * 1024 * 1024) {
      this.toast.notify('L\'image ne doit pas dépasser 2 Mo', 'warning'); return;
    }
    this.settings.setDraftEntreprise({ logo: '' });
    const reader = new FileReader();
    reader.onload = (e) => {
      this.ngZone.run(() => {
        this.settings.setDraftEntreprise({ logo: e.target?.result as string });
        this.toast.notify('Logo mis à jour', 'success');
      });
    };
    reader.readAsDataURL(file);
    input.value = '';
  }

  removeLogo(): void {
    this.settings.setDraftEntreprise({ logo: '' });
    this.toast.notify('Logo supprimé', 'success');
  }

  handleReset(): void {
    if (!confirm('⚠️ Réinitialiser TOUS les paramètres aux valeurs par défaut ? Cette action est irréversible.')) return;
    this.settings.resetSettings();
    this.toast.notify('Paramètres réinitialisés aux valeurs par défaut', 'success');
  }

  async loadSmtpSettings(): Promise<void> {
    this.smtpLoading = true;
    try { this.smtp = await this.api.getSmtpSettings(); }
    catch { /* keep defaults */ }
    finally { this.smtpLoading = false; }
  }

  async handleSaveSmtp(): Promise<void> {
    this.smtpSaving = true;
    try {
      this.smtp = await this.api.updateSmtpSettings(this.smtp);
      this.toast.notify('Configuration SMTP sauvegardée', 'success');
    } catch { this.toast.notify('Erreur lors de la sauvegarde SMTP', 'error'); }
    finally { this.smtpSaving = false; }
  }

  async handleTestSmtp(): Promise<void> {
    if (!this.smtpTestEmail.trim()) { this.toast.notify('Saisissez une adresse email de test', 'warning'); return; }
    this.smtpTesting = true;
    try {
      const res = await this.api.testSmtp(this.smtpTestEmail.trim());
      this.toast.notify(res.message, 'success');
    } catch (e: any) {
      this.toast.notify(e?.error?.message || e?.error?.detail || 'Erreur SMTP', 'error');
    } finally { this.smtpTesting = false; }
  }

  async loadComptes(): Promise<void> {
    this.comptesLoading = true;
    try { this.comptesList = await this.api.utilisateursList(); }
    catch { this.toast.notify('Erreur lors du chargement des comptes', 'error'); }
    finally { this.comptesLoading = false; }
  }

  openCreateCompte(): void {
    this.compteForm = { prenom: '', nom: '', email: '', password: '', role: 'Gestionnaire', telephone: '', isActive: true };
    this.comptesModalMode = 'create';
    this.comptesEditId = null;
    this.comptesModalOpen = true;
  }

  openEditCompte(u: UtilisateurDto): void {
    this.compteForm = { prenom: u.prenom, nom: u.nom, email: u.email, password: '', role: u.role, telephone: u.telephone || '', isActive: u.isActive };
    this.comptesModalMode = 'edit';
    this.comptesEditId = u.id;
    this.comptesModalOpen = true;
  }

  async handleSaveCompte(): Promise<void> {
    if (!this.compteForm.prenom.trim() || !this.compteForm.nom.trim()) {
      this.toast.notify('Prénom et nom sont requis', 'warning'); return;
    }
    if (this.comptesModalMode === 'create' && (!this.compteForm.email.trim() || !this.compteForm.password)) {
      this.toast.notify('Email et mot de passe requis', 'warning'); return;
    }
    this.comptesSaving = true;
    try {
      if (this.comptesModalMode === 'create') {
        const payload: CreateUtilisateurPayload = {
          nom: this.compteForm.nom.trim(), prenom: this.compteForm.prenom.trim(),
          email: this.compteForm.email.trim(), password: this.compteForm.password,
          role: this.compteForm.role, telephone: this.compteForm.telephone.trim() || undefined,
        };
        await this.api.utilisateurCreate(payload);
        this.toast.notify('Compte créé avec succès', 'success');
      } else if (this.comptesEditId != null) {
        const payload: UpdateUtilisateurPayload = {
          nom: this.compteForm.nom.trim(), prenom: this.compteForm.prenom.trim(),
          telephone: this.compteForm.telephone.trim() || undefined,
          role: this.compteForm.role, isActive: this.compteForm.isActive,
        };
        await this.api.utilisateurUpdate(this.comptesEditId, payload);
        this.toast.notify('Compte mis à jour', 'success');
      }
      this.comptesModalOpen = false;
      await this.loadComptes();
    } catch { this.toast.notify('Erreur lors de la sauvegarde', 'error'); }
    finally { this.comptesSaving = false; }
  }

  async handleDeleteCompte(u: UtilisateurDto): Promise<void> {
    if (!confirm(`Supprimer le compte de ${u.nomComplet} ? Cette action est irréversible.`)) return;
    try { await this.api.utilisateurDelete(u.id); this.toast.notify('Compte supprimé', 'success'); await this.loadComptes(); }
    catch { this.toast.notify('Impossible de supprimer ce compte', 'error'); }
  }

  readonly timezones: { value: string; label: string; offset: string }[] = [
    { value: 'Africa/Casablanca',    label: 'Maroc (Casablanca)',         offset: 'UTC+1' },
    { value: 'Africa/Algiers',       label: 'Algérie (Alger)',            offset: 'UTC+1' },
    { value: 'Africa/Tunis',         label: 'Tunisie (Tunis)',            offset: 'UTC+1' },
    { value: 'Africa/Cairo',         label: 'Égypte (Le Caire)',          offset: 'UTC+2' },
    { value: 'Europe/Paris',         label: 'France / Belgique (Paris)',  offset: 'UTC+1/+2' },
    { value: 'Europe/London',        label: 'Royaume-Uni (Londres)',      offset: 'UTC+0/+1' },
    { value: 'Europe/Madrid',        label: 'Espagne (Madrid)',           offset: 'UTC+1/+2' },
    { value: 'Europe/Berlin',        label: 'Allemagne (Berlin)',         offset: 'UTC+1/+2' },
    { value: 'Europe/Istanbul',      label: 'Turquie (Istanbul)',         offset: 'UTC+3' },
    { value: 'Asia/Dubai',           label: 'Émirats (Dubaï)',            offset: 'UTC+4' },
    { value: 'Asia/Riyadh',          label: 'Arabie Saoudite (Riyad)',    offset: 'UTC+3' },
    { value: 'Asia/Beirut',          label: 'Liban (Beyrouth)',           offset: 'UTC+2/+3' },
    { value: 'Asia/Karachi',         label: 'Pakistan (Karachi)',         offset: 'UTC+5' },
    { value: 'Asia/Kolkata',         label: 'Inde (New Delhi)',           offset: 'UTC+5:30' },
    { value: 'Asia/Shanghai',        label: 'Chine (Shanghai)',           offset: 'UTC+8' },
    { value: 'Asia/Tokyo',           label: 'Japon (Tokyo)',              offset: 'UTC+9' },
    { value: 'Australia/Sydney',     label: 'Australie (Sydney)',         offset: 'UTC+10/+11' },
    { value: 'America/New_York',     label: 'USA Est (New York)',         offset: 'UTC-5/-4' },
    { value: 'America/Chicago',      label: 'USA Centre (Chicago)',       offset: 'UTC-6/-5' },
    { value: 'America/Los_Angeles',  label: 'USA Ouest (Los Angeles)',    offset: 'UTC-8/-7' },
    { value: 'America/Sao_Paulo',    label: 'Brésil (São Paulo)',         offset: 'UTC-3' },
    { value: 'UTC',                  label: 'UTC (temps universel)',      offset: 'UTC+0' },
  ];

  getDatePreview(): string {
    const d = new Date();
    const tz = this.settings.settings.timezone || 'Africa/Casablanca';
    const dateParts = new Intl.DateTimeFormat('en-CA', {
      timeZone: tz, year: 'numeric', month: '2-digit', day: '2-digit',
    }).formatToParts(d);
    const get = (t: string) => dateParts.find((p: Intl.DateTimeFormatPart) => p.type === t)?.value ?? '';
    const dd = get('day'); const mm = get('month'); const yyyy = get('year');
    const fmt = this.settings.settings.formatDate;
    if (fmt === 'MM/dd/yyyy') return `${mm}/${dd}/${yyyy}`;
    if (fmt === 'yyyy-MM-dd') return `${yyyy}-${mm}-${dd}`;
    return `${dd}/${mm}/${yyyy}`;
  }

  getTimePreview(): string {
    const d = new Date();
    const tz = this.settings.settings.timezone || 'Africa/Casablanca';
    const h12 = this.settings.settings.heureFormat === '12h';
    return new Intl.DateTimeFormat('fr-FR', {
      timeZone: tz, hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: h12,
    }).format(d);
  }

  getCurrentTimezoneOffset(): string {
    const tz = this.settings.settings.timezone || 'Africa/Casablanca';
    const found = this.timezones.find(t => t.value === tz);
    return found?.offset ?? '';
  }

  get refYear(): number { return new Date().getFullYear(); }

  refPreview(prefix: string, digits: number): string {
    const pad = '1'.padStart(digits, '0');
    return this.settings.settings.facturation.includeAnnee
      ? `${prefix}-${this.refYear}-${pad}`
      : `${prefix}-${pad}`;
  }
}
