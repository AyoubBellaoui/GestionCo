import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { Achat } from '../../core/models';
import { formatNum, formatDate, getInitials, getAvatarClass } from '../../core/utils/format';

@Component({
  selector: 'app-achats',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './achats.component.html',
})
export class AchatsComponent implements OnInit {
  achats: Achat[] = [];
  loading = true;
  search = '';

  formatNum = formatNum;
  formatDate = formatDate;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, public router: Router) {}

  async ngOnInit(): Promise<void> {
    try { this.achats = await this.api.achatsList().catch(() => []); }
    finally { this.loading = false; }
  }

  get filtered(): Achat[] {
    return this.achats.filter(a => {
      if (this.search && !a.reference.toLowerCase().includes(this.search.toLowerCase()) &&
        !(a.nomFournisseur || '').toLowerCase().includes(this.search.toLowerCase())) return false;
      return true;
    });
  }

  get stats() {
    const now = new Date();
    const thisMonth = this.achats.filter(a => {
      const d = new Date(a.dateAchat);
      return d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
    });
    return {
      totalMois: thisMonth.reduce((s, a) => s + a.montantTotal, 0),
      countMois: thisMonth.length,
      fournisseurs: new Set(this.achats.map(a => a.fournisseurId)).size,
      totalGeneral: this.achats.reduce((s, a) => s + a.montantTotal, 0),
    };
  }

  resetFilters(): void { this.search = ''; }
}
