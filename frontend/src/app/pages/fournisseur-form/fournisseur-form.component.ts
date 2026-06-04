import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PhoneInputComponent } from '../../shared/phone-input/phone-input.component';
import { ActivatedRoute, Router } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Fournisseur } from '../../core/models';
import { formatNum } from '../../core/utils/format';

const ICONES = ['🏢', '🏭', '📦', '🚚', '💻', '🪑', '⚡', '🔧', '🛠️', '🏪', '🏬', '📱', '🖨️', '🎨', '🧰'];

@Component({
  selector: 'app-fournisseur-form',
  standalone: true,
  imports: [FormsModule, PhoneInputComponent],
  templateUrl: './fournisseur-form.component.html',
})
export class FournisseurFormComponent implements OnInit {
  id: number | null = null;
  isEdit = false;
  loading = false;
  saving = false;
  icones = ICONES;
  formatNum = formatNum;

  form: Partial<Fournisseur> = {
    nom: '', icone: '🏢', telephone: '', email: '', adresse: '', siteWeb: '', personneContact: '', isActive: true,
  };

  constructor(private api: ApiService, private toast: ToastService, private route: ActivatedRoute, public router: Router) {}

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.id = idParam ? +idParam : null;
    this.isEdit = !!this.id;
    if (this.isEdit && this.id) {
      this.loading = true;
      try { this.form = await this.api.fournisseurGet(this.id); }
      catch { this.toast.notify('Fournisseur introuvable', 'error'); this.router.navigate(['/fournisseurs']); }
      finally { this.loading = false; }
    }
  }

  async save(andNew = false): Promise<void> {
    if (!this.form.nom?.trim()) { this.toast.notify('Nom du fournisseur requis', 'warning'); return; }
    if (this.form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email)) {
      this.toast.notify('Email invalide', 'warning'); return;
    }
    if (this.form.siteWeb && !/^https?:\/\//.test(this.form.siteWeb)) {
      this.form.siteWeb = 'https://' + this.form.siteWeb;
    }
    this.saving = true;
    try {
      if (this.isEdit && this.id) {
        await this.api.fournisseurUpdate(this.id, this.form);
        this.toast.notify('Fournisseur mis à jour avec succès', 'success');
        this.router.navigate(['/fournisseurs']);
      } else {
        await this.api.fournisseurCreate(this.form);
        this.toast.notify('Fournisseur créé avec succès', 'success');
        if (andNew) this.form = { nom: '', icone: '🏢', telephone: '', email: '', adresse: '', siteWeb: '', personneContact: '', isActive: true };
        else this.router.navigate(['/fournisseurs']);
      }
    } catch { this.toast.notify("Erreur d'enregistrement", 'error'); }
    finally { this.saving = false; }
  }
}
