import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, firstValueFrom, map } from 'rxjs';
import {
  User, LoginResponse, DashboardStats, Vente, Achat, Produit, Client,
  Fournisseur, Categorie, Paiement, MouvementStock, Facture, AuditLog,
  UtilisateurDto, CreateUtilisateurPayload, UpdateUtilisateurPayload,
  PagedList, VenteSansFacture
} from '../models';
import { environment } from '../../../environments/environment';

const LIST_PARAMS = new HttpParams().set('page', '1').set('pageSize', '200');

function normalizeList<T>(data: T[] | PagedList<T>): T[] {
  if (Array.isArray(data)) return data;
  if (data && typeof data === 'object' && Array.isArray((data as PagedList<T>).items)) {
    return (data as PagedList<T>).items;
  }
  return [];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // ── AUTH ──
  login(email: string, password: string): Promise<LoginResponse> {
    return firstValueFrom(this.http.post<LoginResponse>(`${this.base}/auth/login`, { email, password }));
  }

  me(): Promise<User> {
    return firstValueFrom(this.http.get<User>(`${this.base}/auth/me`));
  }

  // ── DASHBOARD ──
  dashboardStats(): Promise<DashboardStats> {
    return firstValueFrom(this.http.get<DashboardStats>(`${this.base}/dashboard/stats`));
  }

  // ── VENTES ──
  ventesList(): Promise<Vente[]> {
    return firstValueFrom(
      this.http.get<Vente[] | PagedList<Vente>>(`${this.base}/ventes`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  venteGet(id: number): Promise<Vente> {
    return firstValueFrom(this.http.get<Vente>(`${this.base}/ventes/${id}`));
  }
  venteCreate(data: any): Promise<Vente> {
    return firstValueFrom(this.http.post<Vente>(`${this.base}/ventes`, data));
  }
  venteUpdate(id: number, data: any): Promise<Vente> {
    return firstValueFrom(this.http.put<Vente>(`${this.base}/ventes/${id}`, data));
  }
  venteDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/ventes/${id}`));
  }
  venteCancel(id: number): Promise<Vente> {
    return firstValueFrom(this.http.post<Vente>(`${this.base}/ventes/${id}/cancel`, {}));
  }

  // ── ACHATS ──
  achatsList(): Promise<Achat[]> {
    return firstValueFrom(
      this.http.get<Achat[] | PagedList<Achat>>(`${this.base}/achats`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  achatCreate(data: any): Promise<Achat> {
    return firstValueFrom(this.http.post<Achat>(`${this.base}/achats`, data));
  }

  // ── PRODUITS ──
  produitsList(): Promise<Produit[]> {
    return firstValueFrom(
      this.http.get<Produit[] | PagedList<Produit>>(`${this.base}/produits`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  produitGet(id: number): Promise<Produit> {
    return firstValueFrom(this.http.get<Produit>(`${this.base}/produits/${id}`));
  }
  produitCreate(data: any): Promise<Produit> {
    return firstValueFrom(this.http.post<Produit>(`${this.base}/produits`, data));
  }
  produitUpdate(id: number, data: any): Promise<Produit> {
    return firstValueFrom(this.http.put<Produit>(`${this.base}/produits/${id}`, data));
  }
  produitDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/produits/${id}`));
  }

  // ── CLIENTS ──
  clientsList(): Promise<Client[]> {
    return firstValueFrom(
      this.http.get<Client[] | PagedList<Client>>(`${this.base}/clients`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  clientGet(id: number): Promise<Client> {
    return firstValueFrom(this.http.get<Client>(`${this.base}/clients/${id}`));
  }
  clientCreate(data: any): Promise<Client> {
    return firstValueFrom(this.http.post<Client>(`${this.base}/clients`, data));
  }
  clientUpdate(id: number, data: any): Promise<Client> {
    return firstValueFrom(this.http.put<Client>(`${this.base}/clients/${id}`, data));
  }
  clientDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/clients/${id}`));
  }

  // ── FOURNISSEURS ──
  fournisseursList(): Promise<Fournisseur[]> {
    return firstValueFrom(
      this.http.get<Fournisseur[] | PagedList<Fournisseur>>(`${this.base}/fournisseurs`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  fournisseurGet(id: number): Promise<Fournisseur> {
    return firstValueFrom(this.http.get<Fournisseur>(`${this.base}/fournisseurs/${id}`));
  }
  fournisseurCreate(data: any): Promise<Fournisseur> {
    return firstValueFrom(this.http.post<Fournisseur>(`${this.base}/fournisseurs`, data));
  }
  fournisseurUpdate(id: number, data: any): Promise<Fournisseur> {
    return firstValueFrom(this.http.put<Fournisseur>(`${this.base}/fournisseurs/${id}`, data));
  }
  fournisseurDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/fournisseurs/${id}`));
  }

  // ── CATEGORIES ──
  categoriesList(): Promise<Categorie[]> {
    return firstValueFrom(this.http.get<Categorie[]>(`${this.base}/categories`));
  }
  categorieCreate(data: { nom: string; description?: string; icone?: string }): Promise<Categorie> {
    return firstValueFrom(this.http.post<Categorie>(`${this.base}/categories`, data));
  }
  categorieUpdate(id: number, data: { nom: string; description?: string; icone?: string }): Promise<Categorie> {
    return firstValueFrom(this.http.put<Categorie>(`${this.base}/categories/${id}`, data));
  }
  categorieDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/categories/${id}`));
  }

  // ── PAIEMENTS ──
  paiementsList(): Promise<Paiement[]> {
    return firstValueFrom(
      this.http.get<Paiement[] | PagedList<Paiement>>(`${this.base}/paiements`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }

  // ── MOUVEMENTS STOCK ──
  mouvementsList(): Promise<MouvementStock[]> {
    return firstValueFrom(
      this.http.get<MouvementStock[] | PagedList<MouvementStock>>(`${this.base}/mouvements-stock`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }

  // ── FACTURES ──
  facturesList(): Promise<Facture[]> {
    return firstValueFrom(
      this.http.get<Facture[] | PagedList<Facture>>(`${this.base}/factures`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  factureDownloadPdf(id: number): Promise<Blob> {
    return firstValueFrom(
      this.http.get(`${this.base}/factures/${id}/pdf`, { responseType: 'blob' })
    );
  }
  factureCreate(data: { venteId: number; dateEmission?: string; dateEcheance?: string }): Promise<Facture> {
    return firstValueFrom(this.http.post<Facture>(`${this.base}/factures`, data));
  }
  factureGetVentesSansFacture(): Promise<VenteSansFacture[]> {
    return firstValueFrom(this.http.get<VenteSansFacture[]>(`${this.base}/factures/ventes-sans-facture`));
  }

  // ── AUDIT ──
  auditList(): Promise<AuditLog[]> {
    return firstValueFrom(
      this.http.get<AuditLog[] | PagedList<AuditLog>>(`${this.base}/logs`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }

  // ── UTILISATEURS ──
  utilisateursList(): Promise<UtilisateurDto[]> {
    return firstValueFrom(this.http.get<UtilisateurDto[]>(`${this.base}/utilisateurs`));
  }
  utilisateurCreate(data: CreateUtilisateurPayload): Promise<UtilisateurDto> {
    return firstValueFrom(this.http.post<UtilisateurDto>(`${this.base}/utilisateurs`, data));
  }
  utilisateurUpdate(id: number, data: UpdateUtilisateurPayload): Promise<UtilisateurDto> {
    return firstValueFrom(this.http.put<UtilisateurDto>(`${this.base}/utilisateurs/${id}`, data));
  }
  utilisateurDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/utilisateurs/${id}`));
  }
}
