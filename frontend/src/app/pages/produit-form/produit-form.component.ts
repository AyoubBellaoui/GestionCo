import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Produit, Fournisseur, Categorie } from '../../core/models';
import { formatNum } from '../../core/utils/format';

const TVA_OPTIONS = [0, 7, 14, 20];

@Component({
  selector: 'app-produit-form',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './produit-form.component.html',
})
export class ProduitFormComponent implements OnInit {
  id: number | null = null;
  isEdit = false;
  loading = false;
  saving = false;
  submitted = false;

  form: Partial<Produit> = {
    nom: '', description: '', prixHT: 0, tva: 20, prixTTC: 0,
    prixVenteHT: 0, tvaVente: 20, prixVenteTTC: 0,
    quantiteStock: 0, seuilAlerte: 5, isActive: true,
  };

  categories: Categorie[] = [];
  fournisseurs: Fournisseur[] = [];
  tvaOptions = TVA_OPTIONS;
  formatNum = formatNum;

  constructor(private api: ApiService, private toast: ToastService, private route: ActivatedRoute, public router: Router) {}

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.id = idParam ? +idParam : null;
    this.isEdit = !!this.id;

    const [cats, fours] = await Promise.all([
      this.api.categoriesList().catch(() => []),
      this.api.fournisseursList().catch(() => []),
    ]);
    this.categories = cats;
    this.fournisseurs = fours;

    if (this.isEdit && this.id) {
      this.loading = true;
      try {
        this.form = await this.api.produitGet(this.id);
      } catch { this.toast.notify('Produit introuvable', 'error'); this.router.navigate(['/produits']); }
      finally { this.loading = false; }
    }
  }

  get prixTTC(): number { return (this.form.prixHT ?? 0) * (1 + (this.form.tva ?? 20) / 100); }
  get prixVenteTTC(): number { return (this.form.prixVenteHT ?? 0) * (1 + (this.form.tvaVente ?? 20) / 100); }
  get margeHT(): number { return (this.form.prixVenteHT ?? 0) - (this.form.prixHT ?? 0); }
  get margePct(): number {
    const achat = this.form.prixHT ?? 0;
    return achat > 0 ? Math.round((this.margeHT / achat) * 100) : 0;
  }

  setField(k: keyof Produit, v: any): void {
    (this.form as any)[k] = v;
    if (k === 'prixHT' || k === 'tva') {
      this.form.prixTTC = this.prixTTC;
    }
    if (k === 'prixVenteHT' || k === 'tvaVente') {
      this.form.prixVenteTTC = this.prixVenteTTC;
    }
  }

  onCategorieChange(idStr: string): void {
    const id = idStr ? +idStr : undefined;
    const cat = this.categories.find(c => c.id === id);
    this.form.categorieId = id;
    this.form.categorieNom = cat?.nom || '';
  }

  onFournisseurChange(idStr: string): void {
    const id = idStr ? +idStr : undefined;
    const f = this.fournisseurs.find(x => x.id === id);
    this.form.fournisseurId = id;
    this.form.fournisseurNom = f?.nom || '';
  }

  async save(andNew = false): Promise<void> {
    this.submitted = true;
    if (!this.form.nom?.trim()) { this.toast.notify('Le nom du produit est requis', 'warning'); return; }
    if ((this.form.prixHT ?? 0) <= 0) { this.toast.notify("Le prix d'achat HT doit être > 0", 'warning'); return; }
    if ((this.form.prixVenteHT ?? 0) <= 0) { this.toast.notify('Le prix de vente HT doit être > 0', 'warning'); return; }
    this.saving = true;
    try {
      if (this.isEdit && this.id) {
        await this.api.produitUpdate(this.id, this.form);
        this.toast.notify('Produit mis à jour', 'success');
        this.router.navigate(['/produits']);
      } else {
        await this.api.produitCreate(this.form);
        this.toast.notify('Produit créé', 'success');
        if (andNew) this.form = { nom: '', description: '', prixHT: 0, tva: 20, prixTTC: 0, prixVenteHT: 0, tvaVente: 20, prixVenteTTC: 0, quantiteStock: 0, seuilAlerte: 5, isActive: true };
        else this.router.navigate(['/produits']);
      }
    } catch { this.toast.notify("Erreur d'enregistrement", 'error'); }
    finally { this.saving = false; }
  }
}
