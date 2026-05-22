import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { CategorieCharge, Fournisseur } from '../../core/models';
import { formatNum } from '../../core/utils/format';

@Component({
  selector: 'app-charge-form',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './charge-form.component.html',
})
export class ChargeFormComponent implements OnInit {
  categories: CategorieCharge[] = [];
  fournisseurs: Fournisseur[] = [];
  loadingData = true;
  saving = false;
  submitted = false;

  titre = '';
  description = '';
  montant = 0;
  categorieId: number | '' = '';
  dateCharge = '';
  fournisseurId: number | '' = '';
  justificatif = '';
  paiementInitial = 0;
  methodePaiement = 'Espece';

  estRecurrente = false;
  periodicite = 'Mensuelle';

  newCatNom = '';
  newCatIcone = '📋';
  newCatSaving = false;
  newCatOpen = false;

  readonly emojiOptions = ['🏠','🏢','⚡','💡','🌐','🚗','✈️','👤','🔧','💧','🛡️','📎','📣','📋','💰','💳','🧾','📦','🏥','🎓','☕','🍽️','🎯','🖨️','📱'];

  formatNum = formatNum;
  Math = Math;

  constructor(private api: ApiService, private toast: ToastService, public router: Router) {
    const today = new Date();
    this.dateCharge = `${today.getFullYear()}-${String(today.getMonth()+1).padStart(2,'0')}-${String(today.getDate()).padStart(2,'0')}`;
  }

  async ngOnInit(): Promise<void> {
    const [cats, fournisseurs] = await Promise.all([
      this.api.categoriesChargeList().catch(() => []),
      this.api.fournisseursList().catch(() => []),
    ]);
    this.categories = cats;
    this.fournisseurs = fournisseurs;
    this.loadingData = false;
  }

  get reste(): number { return Math.max(0, this.montant - this.paiementInitial); }
  get selectedCategorie(): CategorieCharge | undefined { return this.categories.find(c => c.id === this.categorieId); }
  get selectedFournisseur(): Fournisseur | undefined { return this.fournisseurs.find(f => f.id === this.fournisseurId); }

  capPaiement(): void {
    this.paiementInitial = Math.min(this.paiementInitial, this.montant);
  }

  async saveNewCat(): Promise<void> {
    if (!this.newCatNom.trim()) { this.toast.notify('Le nom est requis', 'warning'); return; }
    this.newCatSaving = true;
    try {
      const created = await this.api.categorieChargeCreate({ nom: this.newCatNom.trim(), icone: this.newCatIcone });
      this.categories = [...this.categories, created].sort((a, b) => a.nom.localeCompare(b.nom));
      this.categorieId = created.id;
      this.newCatOpen = false;
      this.newCatNom = '';
      this.toast.notify(`Catégorie « ${created.nom} » créée et sélectionnée`, 'success');
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.newCatSaving = false; }
  }

  async save(): Promise<void> {
    this.submitted = true;
    if (!this.titre.trim()) { this.toast.notify('Le titre est requis', 'warning'); return; }
    if (!this.montant || this.montant <= 0) { this.toast.notify('Le montant doit être > 0', 'warning'); return; }
    if (!this.categorieId) { this.toast.notify('Sélectionnez une catégorie', 'warning'); return; }

    this.saving = true;
    try {
      await this.api.chargeCreate({
        titre: this.titre.trim(),
        description: this.description.trim() || undefined,
        montant: this.montant,
        categorieChargeId: this.categorieId as number,
        dateCharge: this.dateCharge || undefined,
        fournisseurId: this.fournisseurId || undefined,
        justificatif: this.justificatif.trim() || undefined,
        paiementInitial: this.paiementInitial > 0 ? this.paiementInitial : undefined,
        methodePaiementInitial: this.paiementInitial > 0 ? this.methodePaiement : undefined,
        estRecurrente: this.estRecurrente,
        periodicite: this.estRecurrente ? this.periodicite : undefined,
      });
      this.toast.notify('Charge créée avec succès', 'success');
      this.router.navigate(['/depenses'], { queryParams: { tab: 'charges' } });
    } catch { this.toast.notify('Erreur lors de la création', 'error'); }
    finally { this.saving = false; }
  }
}
