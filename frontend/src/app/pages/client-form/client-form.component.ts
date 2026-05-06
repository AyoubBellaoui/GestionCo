import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { NgClass } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { ToastService } from '../../core/services/toast.service';
import { Client } from '../../core/models';

const VILLES = ['Casablanca', 'Rabat', 'Marrakech', 'Tanger', 'Agadir', 'Fès', 'Meknès', 'Oujda', 'Tétouan', 'Salé'];

@Component({
  selector: 'app-client-form',
  standalone: true,
  imports: [FormsModule, NgClass],
  templateUrl: './client-form.component.html',
})
export class ClientFormComponent implements OnInit {
  id: number | null = null;
  isEdit = false;
  loading = false;
  saving = false;
  villes = VILLES;

  form: Partial<Client> = {
    nomClient: '', type: 'Entreprise', ice: '', rc: '', adresse: '', ville: '',
    telephone: '', email: '', personneContact: '', isActive: true, sourceAcquisition: '',
  };

  readonly sourcesAcquisition = ['Facebook', 'Instagram', 'WhatsApp', 'Email', 'Recommandation', 'Site web', 'Salon / Événement', 'Autre'];

  constructor(private api: ApiService, private toast: ToastService, private route: ActivatedRoute, public router: Router) {}

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    this.id = idParam ? +idParam : null;
    this.isEdit = !!this.id;
    if (this.isEdit && this.id) {
      this.loading = true;
      try { this.form = await this.api.clientGet(this.id); }
      catch { this.toast.notify('Client introuvable', 'error'); this.router.navigate(['/clients']); }
      finally { this.loading = false; }
    }
  }

  get isEntreprise(): boolean { return this.form.type === 'Entreprise'; }

  get initiales(): string {
    return (this.form.nomClient || '').split(' ').filter(Boolean).slice(0, 2).map(s => s[0]).join('').toUpperCase() || '??';
  }

  async save(andNew = false): Promise<void> {
    if (!this.form.nomClient?.trim()) {
      this.toast.notify(this.isEntreprise ? 'Raison sociale requise' : 'Nom du client requis', 'warning'); return;
    }
    if (this.isEntreprise && this.form.ice && !/^\d{15}$/.test(this.form.ice)) {
      this.toast.notify('ICE doit contenir exactement 15 chiffres', 'warning'); return;
    }
    if (this.form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.form.email)) {
      this.toast.notify('Email invalide', 'warning'); return;
    }
    this.saving = true;
    try {
      if (this.isEdit && this.id) {
        await this.api.clientUpdate(this.id, this.form);
        this.toast.notify('Client mis à jour avec succès', 'success');
        this.router.navigate(['/clients']);
      } else {
        await this.api.clientCreate(this.form);
        this.toast.notify('Client créé avec succès', 'success');
        if (andNew) this.form = { nomClient: '', type: 'Entreprise', ice: '', rc: '', adresse: '', ville: '', telephone: '', email: '', personneContact: '', isActive: true, sourceAcquisition: '' };
        else this.router.navigate(['/clients']);
      }
    } catch { this.toast.notify("Erreur d'enregistrement", 'error'); }
    finally { this.saving = false; }
  }
}
