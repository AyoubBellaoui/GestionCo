import { Component, Input, Output, EventEmitter, OnChanges, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';

interface CalDay {
  d: number;
  iso: string;
  currentMonth: boolean;
  isToday: boolean;
  isMarked: boolean;
}

@Component({
  selector: 'app-date-picker',
  standalone: true,
  imports: [CommonModule],
  styles: [`
    .dp-wrap { position: relative; display: inline-block; }

    .dp-trigger {
      display: flex; align-items: center; gap: 8px;
      padding: 7px 12px; border: 1.5px solid var(--border); border-radius: 8px;
      background: var(--bg-surface); cursor: pointer; color: var(--text);
      font-size: 13px; min-width: 190px; justify-content: flex-start;
      transition: border-color 0.15s; font-family: inherit;
    }
    .dp-trigger:hover { border-color: var(--primary); }
    .dp-trigger.active { border-color: var(--primary); box-shadow: 0 0 0 3px rgba(99,102,241,0.1); }

    .dp-trigger-label { flex: 1; text-align: left; }
    .dp-trigger-label.placeholder { color: var(--text-muted); }

    .dp-clear {
      margin-left: auto; background: none; border: none; cursor: pointer;
      color: var(--text-muted); font-size: 16px; line-height: 1; padding: 0 0 0 4px;
      display: flex; align-items: center;
    }
    .dp-clear:hover { color: var(--danger); }

    .dp-popup {
      position: fixed; z-index: 9999;
      background: var(--bg-surface); border: 1.5px solid var(--border);
      border-radius: 14px; box-shadow: 0 10px 40px rgba(0,0,0,0.18);
      padding: 16px; width: 280px; animation: dpFadeIn 0.12s ease;
    }
    @keyframes dpFadeIn { from { opacity:0; transform:translateY(-6px); } to { opacity:1; transform:translateY(0); } }

    .dp-header {
      display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px;
    }
    .dp-nav {
      width: 30px; height: 30px; border-radius: 7px; border: 1.5px solid var(--border);
      background: var(--bg); cursor: pointer; color: var(--text); font-size: 17px;
      display: flex; align-items: center; justify-content: center; transition: background 0.12s;
    }
    .dp-nav:hover { background: var(--bg-alt, #f1f5f9); }
    .dp-month-lbl { font-weight: 700; font-size: 14px; color: var(--text); text-transform: capitalize; }

    .dp-weekdays {
      display: grid; grid-template-columns: repeat(7, 1fr);
      margin-bottom: 6px; text-align: center;
    }
    .dp-weekdays span { font-size: 11px; color: var(--text-muted); font-weight: 600; padding: 2px 0; }

    .dp-grid { display: grid; grid-template-columns: repeat(7, 1fr); gap: 2px; }

    .dp-day {
      position: relative; display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      height: 34px; border-radius: 7px; border: none;
      background: transparent; cursor: pointer; font-size: 13px;
      color: var(--text); transition: background 0.1s; font-family: inherit;
    }
    .dp-day:hover:not(.selected) { background: var(--bg-alt, #f1f5f9); }
    .dp-day.other-month { color: var(--text-muted); opacity: 0.35; }
    .dp-day.today:not(.selected) { font-weight: 700; box-shadow: inset 0 0 0 1.5px var(--primary); border-radius: 7px; }
    .dp-day.selected { background: var(--primary); color: #fff !important; font-weight: 700; }
    .dp-day.selected:hover { background: var(--primary); }

    .dp-dot {
      width: 4px; height: 4px; border-radius: 50%;
      background: var(--primary); position: absolute; bottom: 4px;
    }
    .dp-day.selected .dp-dot { background: rgba(255,255,255,0.75); }
    .dp-day.other-month .dp-dot { opacity: 0.4; }

    .dp-legend {
      margin-top: 12px; padding-top: 10px; border-top: 1px solid var(--border);
      display: flex; align-items: center; gap: 6px; font-size: 11px; color: var(--text-muted);
    }
    .dp-legend-dot { width: 6px; height: 6px; border-radius: 50%; background: var(--primary); display: inline-block; }
  `],
  template: `
    <div class="dp-wrap">
      <button type="button" [class]="'dp-trigger' + (open ? ' active' : '')" (click)="toggle($event)">
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/>
          <line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>
        </svg>
        <span [class]="'dp-trigger-label' + (!value ? ' placeholder' : '')">
          {{ value ? displayValue : placeholder }}
        </span>
        @if (value) {
          <button type="button" class="dp-clear" (click)="clear($event)" title="Effacer">×</button>
        }
      </button>

      @if (open) {
        <div class="dp-popup" [style.top.px]="popupTop" [style.left.px]="popupLeft">
          <div class="dp-header">
            <button type="button" class="dp-nav" (click)="prevMonth()">‹</button>
            <span class="dp-month-lbl">{{ monthLabel }}</span>
            <button type="button" class="dp-nav" (click)="nextMonth()">›</button>
          </div>

          <div class="dp-weekdays">
            @for (w of weekDays; track w) { <span>{{ w }}</span> }
          </div>

          <div class="dp-grid">
            @for (day of calendarDays; track day.iso) {
              <button type="button"
                [class]="'dp-day'
                  + (!day.currentMonth ? ' other-month' : '')
                  + (day.isToday ? ' today' : '')
                  + (day.iso === value ? ' selected' : '')
                  + (day.isMarked ? ' marked' : '')"
                (click)="selectDay(day)">
                {{ day.d }}
                @if (day.isMarked) { <span class="dp-dot"></span> }
              </button>
            }
          </div>

          @if (markedDates.length > 0) {
            <div class="dp-legend">
              <span class="dp-legend-dot"></span> Jour avec opérations
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class DatePickerComponent implements OnChanges {
  @Input() value = '';
  @Input() markedDates: string[] = [];
  @Input() placeholder = 'Choisir une date';
  @Output() valueChange = new EventEmitter<string>();

  open = false;
  popupTop = 0;
  popupLeft = 0;
  viewYear = new Date().getFullYear();
  viewMonth = new Date().getMonth();

  private markedSet = new Set<string>();

  readonly weekDays = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'];
  readonly months = ['Janvier','Février','Mars','Avril','Mai','Juin','Juillet','Août','Septembre','Octobre','Novembre','Décembre'];

  constructor(private el: ElementRef) {}

  ngOnChanges(): void {
    this.markedSet = new Set(this.markedDates);
    if (this.value) {
      const parts = this.value.split('-');
      this.viewYear = Number(parts[0]);
      this.viewMonth = Number(parts[1]) - 1;
    }
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    if (!this.el.nativeElement.contains(e.target as Node)) this.open = false;
  }

  get displayValue(): string {
    if (!this.value) return '';
    const [y, m, d] = this.value.split('-');
    return `${d}/${m}/${y}`;
  }

  get monthLabel(): string {
    return `${this.months[this.viewMonth]} ${this.viewYear}`;
  }

  get calendarDays(): CalDay[] {
    const days: CalDay[] = [];
    const today = new Date();
    const todayIso = this.toIso(today);
    const firstDay = new Date(this.viewYear, this.viewMonth, 1);
    const lastDayNum = new Date(this.viewYear, this.viewMonth + 1, 0).getDate();
    const startDow = (firstDay.getDay() + 6) % 7; // Mon=0 … Sun=6

    for (let i = startDow - 1; i >= 0; i--) {
      const d = new Date(this.viewYear, this.viewMonth, -i);
      const iso = this.toIso(d);
      days.push({ d: d.getDate(), iso, currentMonth: false, isToday: iso === todayIso, isMarked: this.markedSet.has(iso) });
    }
    for (let i = 1; i <= lastDayNum; i++) {
      const d = new Date(this.viewYear, this.viewMonth, i);
      const iso = this.toIso(d);
      days.push({ d: i, iso, currentMonth: true, isToday: iso === todayIso, isMarked: this.markedSet.has(iso) });
    }
    const rem = 42 - days.length;
    for (let i = 1; i <= rem; i++) {
      const d = new Date(this.viewYear, this.viewMonth + 1, i);
      const iso = this.toIso(d);
      days.push({ d: i, iso, currentMonth: false, isToday: iso === todayIso, isMarked: this.markedSet.has(iso) });
    }
    return days;
  }

  private toIso(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth()+1).padStart(2,'0')}-${String(d.getDate()).padStart(2,'0')}`;
  }

  @HostListener('window:resize')
  onResize(): void { this.open = false; }

  toggle(e: Event): void {
    e.stopPropagation();
    if (!this.open) {
      const trigger = (this.el.nativeElement as HTMLElement).querySelector('.dp-trigger') as HTMLElement;
      const rect = trigger.getBoundingClientRect();
      this.popupTop = rect.bottom + 6;
      const right = rect.left + 280;
      this.popupLeft = right > window.innerWidth ? window.innerWidth - 288 : rect.left;
    }
    this.open = !this.open;
  }

  prevMonth(): void {
    if (this.viewMonth === 0) { this.viewMonth = 11; this.viewYear--; }
    else this.viewMonth--;
  }

  nextMonth(): void {
    if (this.viewMonth === 11) { this.viewMonth = 0; this.viewYear++; }
    else this.viewMonth++;
  }

  selectDay(day: CalDay): void {
    if (!day.currentMonth) {
      const parts = day.iso.split('-');
      this.viewYear = Number(parts[0]);
      this.viewMonth = Number(parts[1]) - 1;
    }
    const newVal = day.iso === this.value ? '' : day.iso;
    this.valueChange.emit(newVal);
    this.open = false;
  }

  clear(e: Event): void {
    e.stopPropagation();
    this.valueChange.emit('');
    this.open = false;
  }
}
