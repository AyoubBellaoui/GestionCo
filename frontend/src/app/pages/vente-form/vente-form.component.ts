import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PhoneInputComponent } from '../../shared/phone-input/phone-input.component';
import { CityInputComponent } from '../../shared/city-input/city-input.component';
import { ActivatedRoute, Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { SettingsService } from '../../core/services/settings.service';
import { Client, Produit, Vente, Commande } from '../../core/models';
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
  imports: [FormsModule, NgClass, PhoneInputComponent, CityInputComponent],
  templateUrl: './vente-form.component.html',
})
export class VenteFormComponent implements OnInit {
  editId: number | null = null;
  sourceCommandeId: number | null = null;
  clients: Client[] = [];
  produits: Produit[] = [];
  loadingData = true;
  clientId: number | '' = '';
  lignes: LigneForm[] = [];
  paiementInitial = 0;
  methodePaiement = 'Espece';
  dateEcheance = '';
  saving = false;
  submitted = false;

  get isEdit(): boolean { return this.editId !== null; }
  get pageTitle(): string { return this.isEdit ? 'Modifier la vente' : this.sourceCommandeId ? 'Convertir en vente' : 'Nouvelle vente'; }

  newClientModalOpen = false;
  newClientSaving = false;
  readonly sourcesAcquisition = ['Facebook', 'Instagram', 'WhatsApp', 'Email', 'Recommandation', 'Site web', 'Salon / Événement', 'Autre'];
  newClientForm: NewClientForm = { nomClient: '', type: 'Particulier', telephone: '', email: '', ville: '', ice: '', personneContact: '', sourceAcquisition: '' };

  formatNum = formatNum;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router, private settings: SettingsService, private route: ActivatedRoute) {}

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
        const vente = await this.api.venteGet(this.editId);
        this.clientId = vente.clientId;
        this.dateEcheance = vente.dateEcheance ? vente.dateEcheance.substring(0, 10) : '';
        this.lignes = vente.lignes.map(l => {
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
            stockDisponible: prod ? prod.quantiteStock + l.quantite : undefined,
          };
        });
      } catch { this.toast.notify('Erreur lors du chargement de la vente', 'error'); }
    } else {
      const commandeIdParam = this.route.snapshot.queryParamMap.get('commandeId');
      if (commandeIdParam) {
        this.sourceCommandeId = +commandeIdParam;
        try {
          const commande: Commande = await this.api.commandeGet(this.sourceCommandeId);
          this.clientId = commande.clientId;
          this.lignes = commande.lignes.map(l => {
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
          this.onClientChange();
        } catch { this.toast.notify('Erreur lors du chargement de la commande', 'error'); }
      } else {
        const dup: Vente | undefined = history.state?.duplicate;
        if (dup?.lignes?.length) {
          this.clientId = dup.clientId;
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
          this.onClientChange();
        }
      }
    }

    this.loadingData = false;
  }

  onClientChange(): void {
    const client = this.selectedClient;
    const delai = client?.delaiPaiement ?? this.settings.settings.facturation.delaiPaiement;
    const d = new Date();
    d.setDate(d.getDate() + delai);
    this.dateEcheance = d.toISOString().slice(0, 10);
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
    if (!quantite || isNaN(quantite)) return;
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
    if (!this.isEdit && this.lignes.some(l => l.stockDisponible === 0)) { this.toast.notify('Un produit est en rupture de stock', 'warning'); return; }
    this.saving = true;
    try {
      if (this.isEdit) {
        await this.api.venteUpdate(this.editId!, {
          clientId: this.clientId,
          lignes: this.lignes.map(l => ({ produitId: l.produitId, quantite: l.quantite, prixUnitaire: l.prixUnitaire, remise: l.remise, tva: l.tva })),
          dateEcheance: this.dateEcheance || undefined,
        });
        this.toast.notify('Vente modifiée avec succès', 'success');
      } else {
        await this.api.venteCreate({
          clientId: this.clientId,
          lignes: this.lignes,
          paiementInitial: this.paiementInitial,
          methodePaiementInitial: this.paiementInitial > 0 ? this.methodePaiement : undefined,
          dateEcheance: this.dateEcheance || undefined,
          commandeId: this.sourceCommandeId || undefined,
        });
        this.toast.notify('Vente créée avec succès', 'success');
      }
      this.router.navigate(['/ventes']);
    } catch { this.toast.notify(this.isEdit ? 'Erreur lors de la modification' : 'Erreur lors de la création', 'error'); }
    finally { this.saving = false; }
  }
}
