import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { Client, Produit } from '../../core/models';
import { formatNum } from '../../core/utils/format';

interface NewClientForm {
  nomClient: string;
  type: string;
  telephone: string;
  email: string;
  ville: string;
  ice: string;
  personneContact: string;
  sourceAcquisition: string;
}

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
  selector: 'app-vente-form',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './vente-form.component.html',
})
export class VenteFormComponent implements OnInit {
  clients: Client[] = [];
  produits: Produit[] = [];
  loadingData = true;
  clientId: number | '' = '';
  lignes: LigneForm[] = [];
  paiementInitial = 0;
  methodePaiement = 'Espece';
  saving = false;
  submitted = false;

  newClientModalOpen = false;
  newClientSaving = false;
  readonly sourcesAcquisition = ['Facebook', 'Instagram', 'WhatsApp', 'Email', 'Recommandation', 'Site web', 'Salon / Événement', 'Autre'];
  newClientForm: NewClientForm = { nomClient: '', type: 'Particulier', telephone: '', email: '', ville: '', ice: '', personneContact: '', sourceAcquisition: '' };

  formatNum = formatNum;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService) {}

  async ngOnInit(): Promise<void> {
    const [c, p] = await Promise.all([
      this.api.clientsList().catch(() => []),
      this.api.produitsList().catch(() => []),
    ]);
    this.clients = c;
    this.produits = p;
    this.loadingData = false;
  }

  get totalHT(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100), 0); }
  get totalTVA(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100) * (l.tva / 100), 0); }
  get totalTTC(): number { return this.totalHT + this.totalTVA; }
  get reste(): number { return this.totalTTC - this.paiementInitial; }
  get selectedClient(): Client | undefined { return this.clients.find(c => c.id === this.clientId); }

  addLigne(): void {
    const tva = this.settings.settings.facturation.tvaParDefaut;
    this.lignes = [...this.lignes, { produitId: 0, quantite: 1, prixUnitaire: 0, remise: 0, tva, total: 0 }];
  }

  private calcTotal(l: LigneForm): number {
    return l.quantite * l.prixUnitaire * (1 - l.remise / 100);
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
      l.quantite = Math.max(1, Math.min(l.quantite, p.quantiteStock));
    }
    l.total = this.calcTotal(l);
    this.lignes = this.lignes.map((x, idx) => idx === i ? l : x);
  }

  updateLigneQty(i: number, quantite: number): void {
    const l = { ...this.lignes[i], quantite };
    if (l.stockDisponible !== undefined) l.quantite = Math.max(1, Math.min(l.quantite, l.stockDisponible));
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

  capPaiement(): void {
    this.paiementInitial = Math.min(this.paiementInitial, this.totalTTC);
  }

  openNewClientModal(): void {
    this.newClientForm = { nomClient: '', type: 'Particulier', telephone: '', email: '', ville: '', ice: '', personneContact: '', sourceAcquisition: '' };
    this.newClientModalOpen = true;
  }

  async saveNewClient(): Promise<void> {
    if (!this.newClientForm.nomClient.trim()) {
      this.toast.notify('Le nom du client est requis', 'warning'); return;
    }
    this.newClientSaving = true;
    try {
      const created = await this.api.clientCreate({
        nomClient: this.newClientForm.nomClient.trim(),
        type: this.newClientForm.type,
        telephone: this.newClientForm.telephone.trim() || undefined,
        email: this.newClientForm.email.trim() || undefined,
        ville: this.newClientForm.ville.trim() || undefined,
        ice: this.newClientForm.ice.trim() || undefined,
        personneContact: this.newClientForm.personneContact.trim() || undefined,
        sourceAcquisition: this.newClientForm.sourceAcquisition.trim() || undefined,
      });
      this.clients = [...this.clients, created];
      this.clientId = created.id;
      this.newClientModalOpen = false;
      this.toast.notify(`Client « ${created.nomClient} » créé et sélectionné`, 'success');
    } catch { this.toast.notify('Erreur lors de la création du client', 'error'); }
    finally { this.newClientSaving = false; }
  }

  async save(): Promise<void> {
    this.submitted = true;
    if (!this.clientId) { this.toast.notify('Sélectionnez un client', 'warning'); return; }
    if (this.lignes.length === 0) { this.toast.notify('Ajoutez au moins une ligne', 'warning'); return; }
    if (this.lignes.some(l => l.produitId === 0)) { this.toast.notify('Sélectionnez un produit pour chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.prixUnitaire <= 0)) { this.toast.notify('Le prix de vente doit être > 0 sur chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.quantite < 1)) { this.toast.notify('La quantité doit être ≥ 1 sur chaque ligne', 'warning'); return; }
    if (this.lignes.some(l => l.stockDisponible === 0)) { this.toast.notify('Un produit est en rupture de stock', 'warning'); return; }
    this.saving = true;
    try {
      await this.api.venteCreate({
        clientId: this.clientId,
        lignes: this.lignes,
        paiementInitial: this.paiementInitial,
        methodePaiementInitial: this.paiementInitial > 0 ? this.methodePaiement : undefined,
      });
      this.toast.notify('Vente créée avec succès', 'success');
      this.router.navigate(['/ventes']);
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.saving = false; }
  }
}
