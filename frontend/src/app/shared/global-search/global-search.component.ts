import {
  Component, OnInit, OnDestroy, HostListener,
  ElementRef, ViewChild, AfterViewInit
} from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter } from 'rxjs/operators';
import { SearchService } from '../../core/services/search.service';
import { ApiService } from '../../core/services/api.service';
import { SearchItem, SearchResults } from '../../core/models';

interface ResultGroup { label: string; icon: string; items: SearchItem[]; }

const BADGE_LABELS: Record<string, string> = {
  Payee: 'Payée', EnAttente: 'En attente', Partiel: 'Partiel',
  Annule: 'Annulé', EnRetard: 'En retard', PartiellementPayee: 'Part. payée',
  Brouillon: 'Brouillon', Envoye: 'Envoyé', Accepte: 'Accepté',
  Refuse: 'Refusé', Converti: 'Converti', Expire: 'Expiré',
  Paye: 'Payé',
};

@Component({
  selector: 'app-global-search',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './global-search.component.html',
})
export class GlobalSearchComponent implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('searchInput') inputRef!: ElementRef<HTMLInputElement>;

  visible = false;
  query   = '';
  loading = false;
  results: SearchResults | null = null;
  groups:  ResultGroup[] = [];
  flatItems: SearchItem[] = [];
  activeIndex = -1;

  private input$   = new Subject<string>();
  private subs     = new Subscription();

  constructor(
    private searchSvc: SearchService,
    private api: ApiService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.subs.add(
      this.searchSvc.open$.subscribe(open => {
        this.visible = open;
        if (open) {
          this.query       = '';
          this.results     = null;
          this.groups      = [];
          this.flatItems   = [];
          this.activeIndex = -1;
          setTimeout(() => this.inputRef?.nativeElement.focus(), 50);
        }
      })
    );

    this.subs.add(
      this.input$.pipe(
        debounceTime(280),
        distinctUntilChanged(),
        filter(q => q.length >= 2 || q.length === 0),
      ).subscribe(q => {
        if (q.length < 2) { this.results = null; this.groups = []; this.flatItems = []; return; }
        this.runSearch(q);
      })
    );
  }

  ngAfterViewInit(): void {}

  ngOnDestroy(): void { this.subs.unsubscribe(); }

  @HostListener('document:keydown', ['$event'])
  onKey(e: KeyboardEvent): void {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      this.searchSvc.open();
      return;
    }
    if (!this.visible) return;

    if (e.key === 'Escape') { this.close(); return; }

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      this.activeIndex = Math.min(this.activeIndex + 1, this.flatItems.length - 1);
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      this.activeIndex = Math.max(this.activeIndex - 1, 0);
    } else if (e.key === 'Enter' && this.activeIndex >= 0) {
      e.preventDefault();
      this.navigate(this.flatItems[this.activeIndex]);
    }
  }

  onInput(val: string): void {
    this.activeIndex = -1;
    this.input$.next(val);
  }

  private async runSearch(q: string): Promise<void> {
    this.loading = true;
    try {
      this.results = await this.api.search(q, 5);
      this.buildGroups();
    } catch { this.results = null; }
    finally { this.loading = false; }
  }

  private buildGroups(): void {
    if (!this.results) { this.groups = []; this.flatItems = []; return; }
    const r = this.results;
    const raw: ResultGroup[] = [
      { label: 'Clients',      icon: '👥', items: r.clients },
      { label: 'Produits',     icon: '📦', items: r.produits },
      { label: 'Ventes',       icon: '🛒', items: r.ventes },
      { label: 'Factures',     icon: '🧾', items: r.factures },
      { label: 'Devis',        icon: '📝', items: r.devis },
      { label: 'Achats',       icon: '📥', items: r.achats },
      { label: 'Fournisseurs', icon: '🚚', items: r.fournisseurs },
    ];
    this.groups    = raw.filter(g => g.items.length > 0);
    this.flatItems = this.groups.flatMap(g => g.items);
    this.activeIndex = this.flatItems.length > 0 ? 0 : -1;
  }

  flatIndex(item: SearchItem): number {
    return this.flatItems.indexOf(item);
  }

  badgeLabel(b?: string): string {
    if (!b) return '';
    return BADGE_LABELS[b] ?? b;
  }

  badgeClass(b?: string): string {
    if (!b) return 'neutral';
    if (['Payee','Accepte','Converti','Paye'].includes(b)) return 'good';
    if (['EnRetard','Annule','Refuse','Expire'].includes(b)) return 'bad';
    if (['Partiel','PartiellementPayee','Envoye','Brouillon'].includes(b)) return 'warn';
    return 'neutral';
  }

  navigate(item: SearchItem): void {
    this.close();
    this.router.navigate([item.route]);
  }

  close(): void { this.searchSvc.close(); }
}
