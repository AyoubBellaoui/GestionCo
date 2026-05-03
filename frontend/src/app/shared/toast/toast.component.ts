import { Component } from '@angular/core';
import { AsyncPipe, NgClass } from '@angular/common';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [AsyncPipe, NgClass],
  templateUrl: './toast.component.html',
})
export class ToastComponent {
  constructor(public toastService: ToastService) {}
}
