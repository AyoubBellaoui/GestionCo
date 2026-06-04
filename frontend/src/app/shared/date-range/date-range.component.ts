import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-date-range',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="dr-wrap">
      <div class="dr-field">
        <span class="dr-label">Du</span>
        <input type="date" class="form-input dr-input"
               [ngModel]="from" (ngModelChange)="from=$event; fromChange.emit($event); rangeChange.emit()" />
      </div>
      <svg class="dr-arrow" width="14" height="14" viewBox="0 0 24 24" fill="none"
           stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/>
      </svg>
      <div class="dr-field">
        <span class="dr-label">Au</span>
        <input type="date" class="form-input dr-input"
               [ngModel]="to" [min]="from" (ngModelChange)="to=$event; toChange.emit($event); rangeChange.emit()" />
      </div>
    </div>
  `,
  styles: [`
    .dr-wrap {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .dr-field {
      display: flex;
      align-items: center;
      gap: 5px;
    }
    .dr-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--text-muted);
      white-space: nowrap;
    }
    .dr-input {
      width: 140px;
      height: 36px;
      font-size: 13px;
      padding: 0 10px;
    }
    .dr-arrow {
      color: var(--text-muted);
      flex-shrink: 0;
    }
  `]
})
export class DateRangeComponent {
  @Input() from = '';
  @Input() to = '';
  @Output() fromChange = new EventEmitter<string>();
  @Output() toChange = new EventEmitter<string>();
  @Output() rangeChange = new EventEmitter<void>();
}
