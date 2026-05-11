import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  constructor(public auth: AuthService) {}

  get initials(): string {
    const user = this.auth.user;
    return user?.initiales ||
      user?.nom?.split(' ').map(p => p[0]).slice(0, 2).join('').toUpperCase() || 'U';
  }

  logout(): void { this.auth.logout(); }
}
