import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Fournisseur } from '../../core/models';
import { formatNum, getInitials, getAvatarClass } from '../../core/utils/format';

@Component({
  selector: 'app-fournisseurs',
  standalone: true,
  imports: [TopbarComponent, FormsModule, NgClass],
  templateUrl: './fournisseurs.component.html',
})
export class FournisseursComponent implements OnInit {
  fournisseurs: Fournisseur[] = [];
  loading = true;
  search = '';

  formatNum = formatNum;
  getInitials = getInitials;
  getAvatarClass = getAvatarClass;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.fournisseurs = await this.api.fournisseursList().catch(() => []); }
    finally { this.loading = false; }
  }

  get filtered(): Fournisseur[] {
    return this.fournisseurs.filter(f =>
      !this.search ||
      f.nom.toLowerCase().includes(this.search.toLowerCase()) ||
      (f.email || '').toLowerCase().includes(this.search.toLowerCase()) ||
      (f.personneContact || '').toLowerCase().includes(this.search.toLowerCase())
    );
  }

  get stats() {
    return {
      total: this.fournisseurs.length,
      achats: this.fournisseurs.reduce((s, f) => s + (f.montantTotalAchats || 0), 0),
      commandes: this.fournisseurs.reduce((s, f) => s + (f.nombreAchats || 0), 0),
      produits: this.fournisseurs.reduce((s, f) => s + (f.nombreProduits || 0), 0),
    };
  }

  async handleDelete(id: number): Promise<void> {
    if (!confirm('Supprimer ce fournisseur ?')) return;
    try { await this.api.fournisseurDelete(id); this.toast.notify('Fournisseur supprimé', 'success'); this.load(); }
    catch { this.toast.notify('Erreur', 'error'); }
  }
}
