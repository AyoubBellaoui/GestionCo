import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';
import { NotificationBellComponent } from '../notification-bell/notification-bell.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [NgIf, NotificationBellComponent],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() icon?: string;
}
