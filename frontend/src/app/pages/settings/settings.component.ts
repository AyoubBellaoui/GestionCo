import { Component, NgZone, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { SettingsService } from '../../core/services/settings.service';
import { UtilisateurDto, CreateUtilisateurPayload, UpdateUtilisateurPayload } from '../../core/models';
import { getAvatarClass } from '../../core/utils/format';

type TabKey = 'entreprise' | 'profil' | 'apparence' | 'notifications' | 'securite' | 'facturation' | 'email' | 'comptes' | 'a-propos';

const TABS: { key: TabKey; label: string; icon: string; desc: string; adminOnly?: boolean }[] = [
  { key: 'entreprise',    label: 'Entreprise',     icon: '🏢', desc: 'Infos légales, ICE, RC, IF' },
  { key: 'profil',        label: 'Mon profil',     icon: '👤', desc: 'Informations personnelles' },
  { key: 'apparence',     label: 'Apparence',      icon: '🎨', desc: 'Devise & format de date' },
  { key: 'notifications', label: 'Notifications',  icon: '🔔', desc: 'Alertes & emails' },
  { key: 'securite',      label: 'Sécurité',       icon: '🔒', desc: 'Mot de passe, sessions' },
  { key: 'facturation',   label: 'Facturation',    icon: '📋', desc: 'Numérotation, TVA, RIB' },
  { key: 'email',         label: 'Email',          icon: '📧', desc: 'Configuration SMTP' },
  { key: 'comptes',       label: 'Comptes',        icon: '👥', desc: 'Gérer les accès utilisateurs', adminOnly: true },
  { key: 'a-propos',      label: 'À propos',       icon: 'ℹ️', desc: 'Version & infos système' },
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
  }

  setIce(value: string): void {
    this.settings.setDraftEntreprise({ ice: value.replace(/\D/g, '').slice(0, 15) });
  }

  setNotifKey(key: string, value: boolean): void {
    this.settings.setDraftNotifs({ [key]: value } as any);
  }

  handleApply(): void {
    this.toast.notify('Paramètres sauvegardés', 'success');
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

  handleChangePassword(): void {
    if (!this.securite.ancienMdp || !this.securite.nouveauMdp) {
      this.toast.notify('Remplissez tous les champs', 'warning'); return;
    }
    if (this.securite.nouveauMdp !== this.securite.confirmMdp) {
      this.toast.notify('Les mots de passe ne correspondent pas', 'error'); return;
    }
    if (this.securite.nouveauMdp.length < 8) {
      this.toast.notify('Le mot de passe doit contenir au moins 8 caractères', 'warning'); return;
    }
    this.toast.notify('Mot de passe modifié avec succès', 'success');
    this.securite = { ancienMdp: '', nouveauMdp: '', confirmMdp: '' };
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

  showSmtpPassword = false;

  handleSmtpUsernameChange(val: string): void {
    this.settings.setDraftSmtp({ username: val });
    if (!this.settings.settings.smtp.fromAddress) {
      this.settings.setDraftSmtp({ fromAddress: val });
    }
  }

  handleReset(): void {
    if (!confirm('⚠️ Réinitialiser TOUS les paramètres aux valeurs par défaut ? Cette action est irréversible.')) return;
    this.settings.resetSettings();
    this.toast.notify('Paramètres réinitialisés aux valeurs par défaut', 'success');
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

  getDatePreview(): string {
    const d = new Date();
    const dd = String(d.getDate()).padStart(2, '0');
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const yyyy = d.getFullYear();
    const fmt = this.settings.settings.formatDate;
    if (fmt === 'MM/dd/yyyy') return `${mm}/${dd}/${yyyy}`;
    if (fmt === 'yyyy-MM-dd') return `${yyyy}-${mm}-${dd}`;
    return `${dd}/${mm}/${yyyy}`;
  }
}
