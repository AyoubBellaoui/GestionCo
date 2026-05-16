import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (total > 0) {
      <div class="pagination">
        <div class="pagination-info">
          Affichage <strong>{{ (page-1)*pageSize+1 }}–{{ min(page*pageSize, total) }}</strong> sur <strong>{{ total }}</strong>
        </div>
        <div class="pagination-controls">
          <select class="page-size" [value]="pageSize" (change)="onPs($any($event.target).value)">
            @for (opt of pageSizeOptions; track opt) {
              <option [value]="opt">{{ opt }} / page</option>
            }
          </select>
          <button class="page-btn" [disabled]="page === 1" (click)="pageChange.emit(page - 1)">←</button>
          @for (p of pages(); track $index) {
            @if (isNum(p)) {
              <button [class]="'page-btn' + (page === p ? ' active' : '')" (click)="pageChange.emit(p)">{{ p }}</button>
            } @else {
              <button class="page-btn" disabled>…</button>
            }
          }
          <button class="page-btn" [disabled]="page >= pageCount" (click)="pageChange.emit(page + 1)">→</button>
        </div>
      </div>
    }
  `
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() pageSize = 10;
  @Input() total = 0;
  @Input() pageSizeOptions = [10, 25, 50];
  @Output() pageChange = new EventEmitter<number>();
  @Output() pageSizeChange = new EventEmitter<number>();

  min = Math.min;

  get pageCount(): number { return Math.max(1, Math.ceil(this.total / this.pageSize)); }

  pages(): (number | '…')[] {
    const n = this.pageCount;
    if (n <= 7) return Array.from({ length: n }, (_, i) => i + 1);
    const list: (number | '…')[] = [1];
    if (this.page > 3) list.push('…');
    for (let i = Math.max(2, this.page - 1); i <= Math.min(n - 1, this.page + 1); i++) list.push(i);
    if (this.page < n - 2) list.push('…');
    list.push(n);
    return list;
  }

  isNum(p: number | '…'): p is number { return typeof p === 'number'; }
  onPs(val: string): void { this.pageSizeChange.emit(Number(val)); }
}
