import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [RouterLink, NotificationBellComponent],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() icon?: string;

  constructor(public auth: AuthService, public theme: ThemeService) {}
}
