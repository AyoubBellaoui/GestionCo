import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIf } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { ThemeService } from '../../core/services/theme.service';
import { ToastComponent } from '../../shared/toast/toast.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, NgIf, ToastComponent],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  email = '';
  password = '';
  showPassword = false;
  remember = false;
  loading = false;
  error: string | null = null;

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router,
    public settings: SettingsService,
    public theme: ThemeService,
  ) {}

  get nomEntreprise(): string { return this.settings.settings.entreprise.raisonSociale || 'GestionCo.'; }
  get logoEntreprise(): string { return this.settings.settings.entreprise.logo || ''; }
  get initialeEntreprise(): string { return (this.nomEntreprise[0] || 'G').toUpperCase(); }

  async handleSubmit(): Promise<void> {
    this.loading = true;
    this.error = null;
    try {
      const res = await this.api.login(this.email, this.password);
      this.auth.setAuth(res.accessToken, res.refreshToken, res.user);
      this.toast.notify('Connexion réussie', 'success');
      this.router.navigate(['/']);
    } catch (err: any) {
      this.error = err?.error?.message || 'Email ou mot de passe incorrect.';
    } finally {
      this.loading = false;
    }
  }

  forgotPassword(): void {
    this.toast.notify('Contactez votre administrateur', 'info');
  }
}
