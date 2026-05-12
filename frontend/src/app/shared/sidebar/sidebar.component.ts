import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SettingsService } from '../../core/services/settings.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  constructor(public auth: AuthService, public settings: SettingsService) {}

  get initials(): string {
    const user = this.auth.user;
    return user?.initiales ||
      user?.nom?.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase() || 'U';
  }

  get companyName(): string {
    return this.settings.settings.entreprise.raisonSociale || 'GestionCo.';
  }

  get companyInitial(): string {
    const name = this.settings.settings.entreprise.raisonSociale;
    return name ? name[0].toUpperCase() : 'G';
  }

  get companyLogo(): string {
    return this.settings.settings.entreprise.logo || '';
  }

  get isAdmin(): boolean { return this.auth.isAdmin; }

  get roleLabel(): string {
    const role = this.auth.user?.role;
    if (role === 'Admin') return 'Administrateur';
    if (role === 'Gestionnaire') return 'Gestionnaire';
    return role || '';
  }

  logout(): void { this.auth.logout(); }
}
