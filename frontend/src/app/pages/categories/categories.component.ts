import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
import { TopbarComponent } from '../../shared/topbar/topbar.component';
import { ModalComponent } from '../../shared/modal/modal.component';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';
import { Categorie } from '../../core/models';
import { formatNum } from '../../core/utils/format';

const ICONES = ['📦', '💻', '📱', '🪑', '⚡', '🎨', '🔧', '🛠️', '📚', '🍔', '👕', '🏠', '🚗', '⌚', '🎮', '💊', '🌱', '🎵'];

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [TopbarComponent, ModalComponent, FormsModule, NgClass],
  templateUrl: './categories.component.html',
})
export class CategoriesComponent implements OnInit {
  categories: Categorie[] = [];
  loading = true;
  search = '';
  showModal = false;
  editing: Categorie | null = null;
  saving = false;
  icones = ICONES;
  formatNum = formatNum;

  form = { nom: '', description: '', icone: '📦' };

  constructor(private api: ApiService, private toast: ToastService, public auth: AuthService) {}

  async ngOnInit(): Promise<void> { await this.load(); }

  async load(): Promise<void> {
    this.loading = true;
    try { this.categories = await this.api.categoriesList(); }
    catch { this.categories = []; }
    finally { this.loading = false; }
  }

  get filtered(): Categorie[] {
    return this.categories.filter(c => !this.search || c.nom.toLowerCase().includes(this.search.toLowerCase()));
  }

  get stats() {
    return {
      total: this.categories.length,
      avecProduits: this.categories.filter(c => (c.nombreProduits || 0) > 0).length,
      totalProduits: this.categories.reduce((s, c) => s + (c.nombreProduits || 0), 0),
    };
  }

  openCreate(): void {
    this.editing = null;
    this.form = { nom: '', description: '', icone: '📦' };
    this.showModal = true;
  }

  openEdit(c: Categorie): void {
    this.editing = c;
    this.form = { nom: c.nom, description: c.description || '', icone: c.icone || '📦' };
    this.showModal = true;
  }

  async save(): Promise<void> {
    if (!this.form.nom.trim()) { this.toast.notify('Nom requis', 'warning'); return; }
    this.saving = true;
    try {
      if (this.editing) {
        await this.api.categorieUpdate(this.editing.id, this.form);
        this.toast.notify('Catégorie mise à jour', 'success');
      } else {
        await this.api.categorieCreate(this.form);
        this.toast.notify('Catégorie créée', 'success');
      }
      this.showModal = false;
      this.load();
    } catch { this.toast.notify('Erreur', 'error'); }
    finally { this.saving = false; }
  }

  async handleDelete(c: Categorie): Promise<void> {
    if ((c.nombreProduits || 0) > 0) {
      this.toast.notify(`Cette catégorie contient ${c.nombreProduits} produit(s). Supprimez-les d'abord.`, 'warning');
      return;
    }
    if (!confirm(`Supprimer la catégorie "${c.nom}" ?`)) return;
    try { await this.api.categorieDelete(c.id); this.toast.notify('Catégorie supprimée', 'success'); this.load(); }
    catch { this.toast.notify('Erreur', 'error'); }
  }
}
