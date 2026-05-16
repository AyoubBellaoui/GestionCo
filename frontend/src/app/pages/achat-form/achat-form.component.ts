import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Fournisseur, Produit } from '../../core/models';
import { formatNum } from '../../core/utils/format';

interface NewFournisseurForm {
  nom: string;
  icone: string;
  telephone: string;
  email: string;
  adresse: string;
  siteWeb: string;
  personneContact: string;
}

interface LigneForm {
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  quantite: number;
  prixUnitaire: number;
  remise: number;
  total: number;
}

@Component({
  selector: 'app-achat-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './achat-form.component.html',
})
export class AchatFormComponent implements OnInit {
  fournisseurs: Fournisseur[] = [];
  produits: Produit[] = [];
  loadingData = true;
  fournisseurId: number | '' = '';
  notes = '';
  lignes: LigneForm[] = [];
  paiementInitial = 0;
  methodePaiement = 'Espece';
  saving = false;
  submitted = false;

  newFournisseurModalOpen = false;
  newFournisseurSaving = false;
  newFournisseurForm: NewFournisseurForm = { nom: '', icone: '🏢', telephone: '', email: '', adresse: '', siteWeb: '', personneContact: '' };

  formatNum = formatNum;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {}

  async ngOnInit(): Promise<void> {
    const [f, p] = await Promise.all([
      this.api.fournisseursList().catch(() => []),
      this.api.produitsList().catch(() => []),
    ]);
    this.fournisseurs = f;
    this.produits = p;
    this.loadingData = false;
  }

  get totalGeneral(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100), 0); }
  get totalArticles(): number { return this.lignes.reduce((s, l) => s + (Number(l.quantite) || 0), 0); }
  get selectedFournisseur(): Fournisseur | undefined { return this.fournisseurs.find(f => f.id === this.fournisseurId); }
  get reste(): number { return this.totalGeneral - this.paiementInitial; }

  capPaiement(): void {
    this.paiementInitial = Math.min(this.paiementInitial, this.totalGeneral);
  }

  private calcTotal(l: LigneForm): number {
    return l.quantite * l.prixUnitaire * (1 - l.remise / 100);
  }

  addLigne(): void {
    this.lignes = [...this.lignes, { produitId: 0, quantite: 1, prixUnitaire: 0, remise: 0, total: 0 }];
  }

  updateLigneProduit(i: number, produitId: number): void {
    const l = { ...this.lignes[i], produitId };
    const p = this.produits.find(x => x.id === produitId);
    if (p) {
      l.prixUnitaire = p.prixHT;
      l.referenceProduit = p.reference;
      l.nomProduit = p.nom;
    }
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneQty(i: number, quantite: number): void {
    const l = { ...this.lignes[i], quantite: quantite || 0 };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLignePrix(i: number, prixUnitaire: number): void {
    const l = { ...this.lignes[i], prixUnitaire: prixUnitaire || 0 };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneRemise(i: number, remise: number): void {
    const l = { ...this.lignes[i], remise: Math.min(100, Math.max(0, remise || 0)) };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  removeLigne(i: number): void { this.lignes = this.lignes.filter((_, idx) => idx !== i); }

  openNewFournisseurModal(): void {
    this.newFournisseurForm = { nom: '', icone: '🏢', telephone: '', email: '', adresse: '', siteWeb: '', personneContact: '' };
    this.newFournisseurModalOpen = true;
  }

  async saveNewFournisseur(): Promise<void> {
    if (!this.newFournisseurForm.nom.trim()) {
      this.toast.notify('Le nom du fournisseur est requis', 'warning'); return;
    }
    this.newFournisseurSaving = true;
    try {
      const created = await this.api.fournisseurCreate({
        nom: this.newFournisseurForm.nom.trim(),
        icone: this.newFournisseurForm.icone || '🏢',
        telephone: this.newFournisseurForm.telephone.trim() || undefined,
        email: this.newFournisseurForm.email.trim() || undefined,
        adresse: this.newFournisseurForm.adresse.trim() || undefined,
        siteWeb: this.newFournisseurForm.siteWeb.trim() || undefined,
        personneContact: this.newFournisseurForm.personneContact.trim() || undefined,
      });
      this.fournisseurs = [...this.fournisseurs, created];
      this.fournisseurId = created.id;
      this.newFournisseurModalOpen = false;
      this.toast.notify(`Fournisseur « ${created.nom} » créé et sélectionné`, 'success');
    } catch { this.toast.notify('Erreur lors de la création du fournisseur', 'error'); }
    finally { this.newFournisseurSaving = false; }
  }

  async save(): Promise<void> {
    this.submitted = true;
    if (!this.fournisseurId) { this.toast.notify('Sélectionnez un fournisseur', 'warning'); return; }
    if (this.lignes.length === 0) { this.toast.notify('Ajoutez au moins une ligne', 'warning'); return; }
    if (this.lignes.some(l => l.produitId === 0)) { this.toast.notify('Sélectionnez un produit pour chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.quantite < 1)) { this.toast.notify('La quantité doit être ≥ 1 sur chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.prixUnitaire <= 0)) { this.toast.notify('Le prix unitaire doit être > 0 sur chaque ligne', 'warning'); return; }
    this.saving = true;
    try {
      await this.api.achatCreate({
        fournisseurId: this.fournisseurId as number,
        notes: this.notes || undefined,
        lignes: this.lignes as any,
        paiementInitial: this.paiementInitial > 0 ? this.paiementInitial : undefined,
        methodePaiementInitial: this.paiementInitial > 0 ? this.methodePaiement : undefined,
      });
      this.toast.notify('Achat créé avec succès — Stock mis à jour', 'success');
      this.router.navigate(['/achats']);
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.saving = false; }
  }
}
