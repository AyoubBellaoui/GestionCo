import { Component, Input, Output, EventEmitter } from '@angular/core';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [NgIf],
  templateUrl: './modal.component.html',
})
export class ModalComponent {
  @Input() open = false;
  @Input() title = '';
  @Input() large = false;
  @Output() closed = new EventEmitter<void>();

  close(): void { this.closed.emit(); }
  stopPropagation(e: Event): void { e.stopPropagation(); }
}
