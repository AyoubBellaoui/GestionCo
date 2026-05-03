import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

const NAV = [
  { to: '/', icon: '📊', label: 'Tableau de bord', end: true },
  { to: '/ventes', icon: '🛒', label: 'Ventes', end: false },
  { to: '/achats', icon: '📥', label: 'Achats', end: false },
  { to: '/produits', icon: '📦', label: 'Produits', end: false },
  { to: '/categories', icon: '📂', label: 'Catégories', end: false },
  { to: '/mouvements-stock', icon: '📈', label: 'Mouvements stock', end: false },
  { to: '/clients', icon: '👥', label: 'Clients', end: false },
  { to: '/fournisseurs', icon: '🚚', label: 'Fournisseurs', end: false },
  { to: '/paiements', icon: '💰', label: 'Paiements', end: false },
  { to: '/factures', icon: '📋', label: 'Factures', end: false },
];

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  navItems = NAV;

  constructor(public auth: AuthService) {}

  get initials(): string {
    const user = this.auth.user;
    return user?.initiales ||
      user?.nom?.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase() || 'U';
  }

  logout(): void { this.auth.logout(); }
}
