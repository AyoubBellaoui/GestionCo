import { Component, Input } from '@angular/core';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [NgIf],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() icon?: string;
}
