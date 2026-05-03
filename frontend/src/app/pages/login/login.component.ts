import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgIf } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
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
  lang: 'FR' | 'EN' | 'AR' = 'FR';

  constructor(
    private api: ApiService,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router
  ) {}

  async handleSubmit(): Promise<void> {
    this.loading = true;
    this.error = null;
    try {
      const res = await this.api.login(this.email, this.password);
      this.auth.setAuth(res.accessToken, res.user);
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
