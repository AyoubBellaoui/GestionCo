import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
  selector: 'app-devis-form',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './devis-form.component.html',
})
export class DevisFormComponent implements OnInit {
  editId: number | null = null;
  clients: Client[] = [];
  produits: Produit[] = [];
  loadingData = true;

  clientId: number | '' = '';
  dateValidite = '';
  notes = '';
  lignes: LigneForm[] = [];
  saving = false;
  submitted = false;

  newClientModalOpen = false;
  newClientSaving = false;
  readonly sourcesAcquisition = ['Facebook', 'Instagram', 'WhatsApp', 'Email', 'Recommandation', 'Site web', 'Salon / Événement', 'Autre'];
  newClientForm: NewClientForm = { nomClient: '', type: 'Particulier', telephone: '', email: '', ville: '', ice: '', personneContact: '', sourceAcquisition: '' };

  formatNum = formatNum;
  Math = Math;

  get isEdit(): boolean { return this.editId !== null; }
  get title(): string { return this.isEdit ? 'Modifier le devis' : 'Nouveau devis'; }

  get totalHT(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100), 0); }
  get totalTVA(): number { return this.lignes.reduce((s, l) => s + l.quantite * l.prixUnitaire * (1 - l.remise / 100) * (l.tva / 100), 0); }
  get totalTTC(): number { return this.totalHT + this.totalTVA; }
  get selectedClient(): Client | undefined { return this.clients.find(c => c.id === this.clientId); }

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
        const devis = await this.api.devisGet(this.editId);
        this.clientId = devis.clientId;
        this.dateValidite = devis.dateValidite ? devis.dateValidite.substring(0, 10) : '';
        this.notes = devis.notes || '';
        this.lignes = devis.lignes.map(l => ({
          produitId: l.produitId,
          nomProduit: l.nomProduit,
          referenceProduit: l.referenceProduit,
          quantite: l.quantite,
          prixUnitaire: l.prixUnitaire,
          remise: l.remise ?? 0,
          prixReference: this.produits.find(p => p.id === l.produitId)?.prixHT,
          tva: l.tva,
          total: l.total,
        }));
      } catch { this.toast.notify('Erreur lors du chargement du devis', 'error'); }
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

    this.saving = true;
    const payload = {
      clientId: this.clientId,
      dateValidite: this.dateValidite || undefined,
      notes: this.notes.trim() || undefined,
      lignes: this.lignes.map(l => ({
        produitId: l.produitId,
        quantite: l.quantite,
        prixUnitaire: l.prixUnitaire,
        tva: l.tva,
      })),
    };

    try {
      if (this.isEdit) {
        await this.api.devisUpdate(this.editId!, payload);
        this.toast.notify('Devis modifié avec succès', 'success');
      } else {
        await this.api.devisCreate(payload);
        this.toast.notify('Devis créé avec succès', 'success');
      }
      this.router.navigate(['/devis']);
    } catch { this.toast.notify('Erreur lors de l\'enregistrement', 'error'); }
    finally { this.saving = false; }
  }
}
