import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { Client, Produit, Commande } from '../../core/models';
import { formatNum } from '../../core/utils/format';

interface LigneForm {
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  quantite: number;
  prixUnitaire: number;
  remise: number;
  prixReference?: number;
  tva: number;
  total: number;
  stockDisponible?: number;
}

@Component({
  selector: 'app-commande-form',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './commande-form.component.html',
})
export class CommandeFormComponent implements OnInit {
  editId: number | null = null;
  clients: Client[] = [];
  produits: Produit[] = [];
  loadingData = true;

  clientId: number | '' = '';
  dateLivraison = '';
  notes = '';
  lignes: LigneForm[] = [];
  saving = false;
  submitted = false;

  formatNum = formatNum;
  Math = Math;

  get isEdit(): boolean { return this.editId !== null; }
  get title(): string { return this.isEdit ? 'Modifier la commande' : 'Nouvelle commande'; }

  get totalHT(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100), 0); }
  get totalTVA(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100) * (l.tva / 100), 0); }
  get totalTTC(): number { return this.totalHT + this.totalTVA; }
  get selectedClient(): Client | undefined { return this.clients.find(c => c.id === this.clientId); }
  get devise(): string { return this.settings.settings.devise || 'MAD'; }

  constructor(
    private api: ApiService,
    private toast: ToastService,
    private route: ActivatedRoute,
    public router: Router,
    private settings: SettingsService
  ) {}

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) this.editId = +idParam;

    const [c, p] = await Promise.all([
      this.api.clientsList().catch(() => []),
      this.api.produitsList().catch(() => []),
    ]);
    this.clients = c;
    this.produits = p;

    if (this.editId) {
      try {
        const commande = await this.api.commandeGet(this.editId);
        this.clientId = commande.clientId;
        this.dateLivraison = commande.dateLivraison ? commande.dateLivraison.substring(0, 10) : '';
        this.notes = commande.notes || '';
        this.lignes = commande.lignes.map(l => ({
          produitId: l.produitId,
          nomProduit: l.nomProduit,
          referenceProduit: l.referenceProduit,
          quantite: l.quantite,
          prixUnitaire: l.prixUnitaire,
          remise: l.remise ?? 0,
          prixReference: this.produits.find(x => x.id === l.produitId)?.prixHT,
          tva: l.tva,
          total: l.total,
        }));
      } catch { this.toast.notify('Erreur lors du chargement de la commande', 'error'); }
    } else {
      const dup: Commande | undefined = history.state?.duplicate;
      if (dup?.lignes?.length) {
        this.clientId = dup.clientId;
        this.notes = dup.notes || '';
        this.lignes = dup.lignes.map(l => {
          const prod = p.find(x => x.id === l.produitId);
          return {
            produitId: l.produitId,
            nomProduit: l.nomProduit,
            referenceProduit: l.referenceProduit,
            quantite: l.quantite,
            prixUnitaire: l.prixUnitaire,
            remise: l.remise ?? 0,
            prixReference: prod?.prixTTC,
            tva: l.tva,
            total: l.total,
            stockDisponible: prod?.quantiteStock,
          };
        });
      }
    }

    this.loadingData = false;
  }

  private calcTotal(l: LigneForm): number {
    return l.quantite * l.prixUnitaire * (1 - l.remise / 100);
  }

  addLigne(): void {
    const tva = this.settings.settings.facturation.tvaParDefaut;
    this.lignes = [...this.lignes, { produitId: 0, quantite: 1, prixUnitaire: 0, remise: 0, tva, total: 0 }];
  }

  updateLigneProduit(i: number, produitId: number): void {
    const l = { ...this.lignes[i], produitId };
    const p = this.produits.find(x => x.id === produitId);
    if (p) {
      l.prixReference = p.prixTTC;
      l.prixUnitaire = p.prixVenteHT;
      l.tva = p.tvaVente;
      l.referenceProduit = p.reference;
      l.nomProduit = p.nom;
      l.stockDisponible = p.quantiteStock;
    }
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneQty(i: number, quantite: number): void {
    const l = { ...this.lignes[i], quantite: Math.max(1, quantite) };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLignePrix(i: number, prixUnitaire: number): void {
    const l = { ...this.lignes[i], prixUnitaire };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneRemise(i: number, remise: number): void {
    const l = { ...this.lignes[i], remise: Math.min(100, Math.max(0, remise || 0)) };
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneTva(i: number, tva: number): void {
    this.lignes = this.lignes.map((x, idx) => idx === i ? { ...x, tva } : x);
  }

  removeLigne(i: number): void { this.lignes = this.lignes.filter((_, idx) => idx !== i); }

  async save(): Promise<void> {
    this.submitted = true;
    if (!this.clientId) { this.toast.notify('Sélectionnez un client', 'warning'); return; }
    if (this.lignes.length === 0) { this.toast.notify('Ajoutez au moins une ligne', 'warning'); return; }
    if (this.lignes.some(l => l.produitId === 0)) { this.toast.notify('Sélectionnez un produit pour chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.prixUnitaire <= 0)) { this.toast.notify('Le prix doit être > 0 sur chaque ligne', 'warning'); return; }

    this.saving = true;
    const payload = {
      clientId: this.clientId,
      dateLivraison: this.dateLivraison || undefined,
      notes: this.notes.trim() || undefined,
      lignes: this.lignes.map(l => ({
        produitId: l.produitId,
        quantite: l.quantite,
        prixUnitaire: l.prixUnitaire,
        remise: l.remise,
        tva: l.tva,
      })),
    };

    try {
      if (this.isEdit) {
        await this.api.commandeUpdate(this.editId!, payload);
        this.toast.notify('Commande modifiée avec succès', 'success');
      } else {
        await this.api.commandeCreate(payload);
        this.toast.notify('Commande créée avec succès', 'success');
      }
      this.router.navigate(['/commandes']);
    } catch (e: any) { this.toast.notify(e?.error?.message || 'Erreur lors de l\'enregistrement', 'error'); }
    finally { this.saving = false; }
  }
}
