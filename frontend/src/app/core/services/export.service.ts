import { Injectable } from '@angular/core';
import * as XLSX from 'xlsx';
import { Vente, Achat, Produit } from '../models';

@Injectable({ providedIn: 'root' })
export class ExportService {

  private download(wb: XLSX.WorkBook, filename: string): void {
    XLSX.writeFile(wb, filename);
  }

  exportVentes(ventes: Vente[]): void {
    const rows = ventes.map(v => ({
      'Référence':        v.reference,
      'Date':             new Date(v.dateVente).toLocaleDateString('fr-FR'),
      'Client':           v.nomClient,
      'Articles':         v.nombreArticles,
      'Montant HT (MAD)': v.montantTotalHT,
      'TVA (MAD)':        v.montantTVA,
      'Montant TTC (MAD)':v.montantTotal,
      'Montant payé (MAD)':v.montantPaye,
      'Reste dû (MAD)':   v.reste,
      'Statut':           this.venteStatut(v),
      'N° Facture':       v.numeroFacture || '—',
      'Utilisateur':      v.nomUtilisateur,
    }));

    const ws = XLSX.utils.json_to_sheet(rows);
    this.autoWidth(ws, rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Ventes');
    this.download(wb, `Ventes_${this.today()}.xlsx`);
  }

  exportAchats(achats: Achat[]): void {
    const rows = achats.map(a => ({
      'Référence':         a.reference,
      'Date':              new Date(a.dateAchat).toLocaleDateString('fr-FR'),
      'Fournisseur':       a.nomFournisseur,
      'Articles':          a.nombreArticles,
      'Montant total (MAD)':a.montantTotal,
      'Montant payé (MAD)':a.montantPaye,
      'Reste dû (MAD)':    a.reste,
      'Statut':            this.achatStatut(a.statut),
      'Utilisateur':       a.nomUtilisateur,
      'Notes':             a.notes || '',
    }));

    const ws = XLSX.utils.json_to_sheet(rows);
    this.autoWidth(ws, rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Achats');
    this.download(wb, `Achats_${this.today()}.xlsx`);
  }

  exportProduits(produits: Produit[]): void {
    const rows = produits.map(p => ({
      'Référence':       p.reference,
      'Nom':             p.nom,
      'Description':     p.description || '',
      'Catégorie':       p.categorieNom || '—',
      'Fournisseur':     p.fournisseurNom || '—',
      'Prix HT (MAD)':   p.prixHT,
      'TVA (%)':         p.tva,
      'Prix TTC (MAD)':  p.prixTTC,
      'Stock':           p.quantiteStock,
      'Seuil alerte':    p.seuilAlerte,
      'État stock':      p.isRupture ? 'Rupture' : p.isStockFaible ? 'Faible' : 'Disponible',
      'Actif':           p.isActive ? 'Oui' : 'Non',
    }));

    const ws = XLSX.utils.json_to_sheet(rows);
    this.autoWidth(ws, rows);
    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, 'Produits');
    this.download(wb, `Produits_${this.today()}.xlsx`);
  }

  private autoWidth(ws: XLSX.WorkSheet, rows: Record<string, any>[]): void {
    if (!rows.length) return;
    const cols = Object.keys(rows[0]).map(key => ({
      wch: Math.max(key.length, ...rows.map(r => String(r[key] ?? '').length)) + 2
    }));
    ws['!cols'] = cols;
  }

  private today(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private venteStatut(v: Vente): string {
    if (v.statut === 'Paye') return 'Payé';
    if (v.montantPaye > 0) return 'Partiel';
    return 'En attente';
  }

  private achatStatut(s: string): string {
    const map: Record<string, string> = { Paye: 'Payé', Partiel: 'Partiel', EnAttente: 'En attente', Annule: 'Annulé' };
    return map[s] || s;
  }
}
