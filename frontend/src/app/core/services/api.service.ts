import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { firstValueFrom, map } from 'rxjs';
import {
  User, LoginResponse, DashboardStats, FullDashboard, Vente, Achat, Produit, Client,
  Fournisseur, Categorie, Paiement, PaiementAchat, MouvementStock, Facture, AuditLog,
  UtilisateurDto, CreateUtilisateurPayload, UpdateUtilisateurPayload,
  PagedList, VenteSansFacture, Charge, CategorieCharge, PaiementCharge,
  AppNotification, NotificationSummary, Devis, ConversionDevisResult,
  PLReport, TVAReport, BalanceAgeeReport, PerformanceCommerciale, SearchResults,
  FacturationSettings, ClientsStats, FournisseursStats, FacturesStats, DevisStats,
  ChargesStats, MouvementsStats, EntrepriseSettings
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

function buildParams(obj: Record<string, any>): HttpParams {
  let p = new HttpParams();
  for (const [k, v] of Object.entries(obj)) {
    if (v !== undefined && v !== null && v !== '') p = p.set(k, String(v));
  }
  return p;
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
  updateProfile(data: { prenom: string; nom: string; telephone?: string }): Promise<User> {
    return firstValueFrom(this.http.put<User>(`${this.base}/auth/profile`, data));
  }
  changePassword(data: { currentPassword: string; newPassword: string }): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/auth/change-password`, data));
  }

  // ── DASHBOARD ──
  dashboardStats(): Promise<DashboardStats> {
    return firstValueFrom(this.http.get<DashboardStats>(`${this.base}/dashboard/stats`));
  }
  dashboardFull(): Promise<FullDashboard> {
    return firstValueFrom(this.http.get<FullDashboard>(`${this.base}/dashboard/full`));
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
  venteAddPaiement(id: number, data: any): Promise<Vente> {
    return firstValueFrom(this.http.post<Vente>(`${this.base}/ventes/${id}/paiements`, data));
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
  achatAddPaiement(id: number, data: any): Promise<Achat> {
    return firstValueFrom(this.http.post<Achat>(`${this.base}/achats/${id}/paiements`, data));
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
  produitBulkImport(items: any[]): Promise<{ imported: number; failed: number; errors: { row: number; message: string }[] }> {
    return firstValueFrom(this.http.post<any>(`${this.base}/produits/import`, items));
  }
  produitGenererReappro(id: number): Promise<{ created: boolean; achatId: number; reference: string; message?: string }> {
    return firstValueFrom(this.http.post<any>(`${this.base}/produits/${id}/reappro`, {}));
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
  clientBulkImport(items: any[]): Promise<{ imported: number; failed: number; errors: { row: number; message: string }[] }> {
    return firstValueFrom(this.http.post<any>(`${this.base}/clients/import`, items));
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
  fournisseurBulkImport(items: any[]): Promise<{ imported: number; failed: number; errors: { row: number; message: string }[] }> {
    return firstValueFrom(this.http.post<any>(`${this.base}/fournisseurs/import`, items));
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

  paiementsAchatList(): Promise<PaiementAchat[]> {
    return firstValueFrom(
      this.http.get<PaiementAchat[] | PagedList<PaiementAchat>>(`${this.base}/achats/paiements`, { params: LIST_PARAMS })
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

  ajustementStock(data: { produitId: number; type: string; quantite: number; raison: string; commentaire?: string }): Promise<MouvementStock> {
    return firstValueFrom(this.http.post<MouvementStock>(`${this.base}/mouvements-stock/ajustement`, data));
  }

  // ── FACTURES ──
  facturesList(): Promise<Facture[]> {
    return firstValueFrom(
      this.http.get<Facture[] | PagedList<Facture>>(`${this.base}/factures`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  factureDownloadPdf(id: number, entreprise?: Record<string, string>): Promise<Blob> {
    return firstValueFrom(
      this.http.post(`${this.base}/factures/${id}/pdf`, entreprise ?? {}, { responseType: 'blob' })
    );
  }
  factureCreate(data: { venteId: number; dateEmission?: string; dateEcheance?: string }): Promise<Facture> {
    return firstValueFrom(this.http.post<Facture>(`${this.base}/factures`, data));
  }
  factureGetVentesSansFacture(): Promise<VenteSansFacture[]> {
    return firstValueFrom(this.http.get<VenteSansFacture[]>(`${this.base}/factures/ventes-sans-facture`));
  }

  // ── CHARGES ──
  chargesList(): Promise<Charge[]> {
    return firstValueFrom(
      this.http.get<Charge[] | PagedList<Charge>>(`${this.base}/charges`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  chargeGet(id: number): Promise<Charge> {
    return firstValueFrom(this.http.get<Charge>(`${this.base}/charges/${id}`));
  }
  chargeCreate(data: any): Promise<Charge> {
    return firstValueFrom(this.http.post<Charge>(`${this.base}/charges`, data));
  }
  chargeUpdate(id: number, data: any): Promise<Charge> {
    return firstValueFrom(this.http.put<Charge>(`${this.base}/charges/${id}`, data));
  }
  chargeDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/charges/${id}`));
  }
  chargeAddPaiement(id: number, data: any): Promise<Charge> {
    return firstValueFrom(this.http.post<Charge>(`${this.base}/charges/${id}/paiements`, data));
  }
  paiementsChargeList(): Promise<PaiementCharge[]> {
    return firstValueFrom(
      this.http.get<PaiementCharge[] | PagedList<PaiementCharge>>(`${this.base}/charges/paiements`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  chargeGenererRecurrentes(): Promise<{ count: number; message: string }> {
    return firstValueFrom(this.http.post<{ count: number; message: string }>(`${this.base}/charges/generer-recurrentes`, {}));
  }

  // ── CATEGORIES CHARGE ──
  categoriesChargeList(): Promise<CategorieCharge[]> {
    return firstValueFrom(this.http.get<CategorieCharge[]>(`${this.base}/categories-charge`));
  }
  categorieChargeCreate(data: { nom: string; icone?: string }): Promise<CategorieCharge> {
    return firstValueFrom(this.http.post<CategorieCharge>(`${this.base}/categories-charge`, data));
  }
  categorieChargeDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/categories-charge/${id}`));
  }

  // ── NOTIFICATIONS ──
  notificationsSummary(limit = 30): Promise<NotificationSummary> {
    return firstValueFrom(this.http.get<NotificationSummary>(`${this.base}/notifications?limit=${limit}`));
  }
  notificationMarkRead(id: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/notifications/${id}/read`, {}));
  }
  notificationMarkAllRead(): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/notifications/read-all`, {}));
  }
  notificationDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/notifications/${id}`));
  }
  notificationDeleteAllRead(): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/notifications/read`));
  }

  // ── DEVIS ──
  devisList(): Promise<Devis[]> {
    return firstValueFrom(
      this.http.get<Devis[] | PagedList<Devis>>(`${this.base}/devis`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }
  devisGet(id: number): Promise<Devis> {
    return firstValueFrom(this.http.get<Devis>(`${this.base}/devis/${id}`));
  }
  devisCreate(data: any): Promise<Devis> {
    return firstValueFrom(this.http.post<Devis>(`${this.base}/devis`, data));
  }
  devisUpdate(id: number, data: any): Promise<Devis> {
    return firstValueFrom(this.http.put<Devis>(`${this.base}/devis/${id}`, data));
  }
  devisUpdateStatut(id: number, statut: string): Promise<Devis> {
    return firstValueFrom(this.http.put<Devis>(`${this.base}/devis/${id}/statut`, { statut }));
  }
  devisConvertir(id: number): Promise<ConversionDevisResult> {
    return firstValueFrom(this.http.post<ConversionDevisResult>(`${this.base}/devis/${id}/convertir`, {}));
  }
  devisDelete(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`${this.base}/devis/${id}`));
  }
  devisPdf(id: number, entreprise?: Record<string, string>): Promise<Blob> {
    return firstValueFrom(this.http.post(`${this.base}/devis/${id}/pdf`, entreprise ?? {}, { responseType: 'blob' }));
  }
  devisShareLink(id: number): Promise<{ url: string }> {
    return firstValueFrom(this.http.get<{ url: string }>(`${this.base}/devis/${id}/share-link`));
  }
  devisEmail(id: number, email: string, message?: string, entreprise?: Record<string, string>): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/devis/${id}/email`, { email, message, entrepriseInfo: entreprise ?? {} }));
  }
  factureEmail(id: number, email: string, message?: string, entreprise?: Record<string, string>): Promise<void> {
    return firstValueFrom(this.http.post<void>(`${this.base}/factures/${id}/email`, { email, message, entrepriseInfo: entreprise ?? {} }));
  }

  // ── AUDIT ──
  auditList(): Promise<AuditLog[]> {
    return firstValueFrom(
      this.http.get<AuditLog[] | PagedList<AuditLog>>(`${this.base}/logs`, { params: LIST_PARAMS })
        .pipe(map(normalizeList))
    );
  }

  // ── UTILISATEURS ──
  // ── RAPPORTS ──
  rapportCashFlow(annee: number, mois: number): Promise<any> {
    return firstValueFrom(this.http.get<any>(`${this.base}/reports/cashflow/${annee}/${mois}`));
  }
  rapportPL(annee: number): Promise<PLReport> {
    return firstValueFrom(this.http.get<PLReport>(`${this.base}/reports/pl/${annee}`));
  }
  rapportTVA(annee: number): Promise<TVAReport> {
    return firstValueFrom(this.http.get<TVAReport>(`${this.base}/reports/tva/${annee}`));
  }
  rapportPdf(type: 'pl' | 'tva', annee: number): Promise<ArrayBuffer> {
    return firstValueFrom(this.http.get(`${this.base}/reports/${type}/${annee}/pdf`, { responseType: 'arraybuffer' }));
  }
  rapportBalanceAgee(): Promise<BalanceAgeeReport> {
    return firstValueFrom(this.http.get<BalanceAgeeReport>(`${this.base}/reports/balance-agee`));
  }
  rapportPerformance(annee: number): Promise<PerformanceCommerciale> {
    return firstValueFrom(this.http.get<PerformanceCommerciale>(`${this.base}/reports/performance/${annee}`));
  }
  search(q: string, take = 5): Promise<SearchResults> {
    return firstValueFrom(this.http.get<SearchResults>(`${this.base}/search`, { params: { q, take } }));
  }

  // ── PAGED LIST METHODS ──
  ventesListPaged(p: { page?: number; pageSize?: number; search?: string; statut?: string; clientId?: number; dateDebut?: string; dateFin?: string } = {}): Promise<PagedList<Vente>> {
    return firstValueFrom(this.http.get<PagedList<Vente>>(`${this.base}/ventes`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  venteGetDates(): Promise<string[]> {
    return firstValueFrom(this.http.get<string[]>(`${this.base}/ventes/dates`));
  }
  achatsListPaged(p: { page?: number; pageSize?: number; search?: string; fournisseurId?: number; dateDebut?: string; dateFin?: string; statut?: string } = {}): Promise<PagedList<Achat>> {
    return firstValueFrom(this.http.get<PagedList<Achat>>(`${this.base}/achats`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  facturesListPaged(p: { page?: number; pageSize?: number; search?: string; statut?: string; clientId?: number; dateFilter?: string; estEnRetard?: boolean } = {}): Promise<PagedList<Facture>> {
    return firstValueFrom(this.http.get<PagedList<Facture>>(`${this.base}/factures`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  clientsListPaged(p: { page?: number; pageSize?: number; search?: string; type?: string } = {}): Promise<PagedList<Client>> {
    return firstValueFrom(this.http.get<PagedList<Client>>(`${this.base}/clients`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  produitsListPaged(p: { page?: number; pageSize?: number; search?: string; categorieId?: number; stockFaibleOnly?: boolean; ruptureOnly?: boolean; disponibleOnly?: boolean; sortBy?: string; sortDesc?: boolean } = {}): Promise<PagedList<Produit>> {
    return firstValueFrom(this.http.get<PagedList<Produit>>(`${this.base}/produits`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  produitsStats(): Promise<{ totalProduits: number; produitsActifs: number; produitsStockFaible: number; produitsRupture: number; valeurTotaleStock: number }> {
    return firstValueFrom(this.http.get<any>(`${this.base}/produits/stats`));
  }
  fournisseursListPaged(p: { page?: number; pageSize?: number; search?: string } = {}): Promise<PagedList<Fournisseur>> {
    return firstValueFrom(this.http.get<PagedList<Fournisseur>>(`${this.base}/fournisseurs`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  chargesListPaged(p: { page?: number; pageSize?: number; search?: string; statut?: string; categorieId?: number; dateDebut?: string; dateFin?: string } = {}): Promise<PagedList<Charge>> {
    return firstValueFrom(this.http.get<PagedList<Charge>>(`${this.base}/charges`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  devisListPaged(p: { page?: number; pageSize?: number; search?: string; statut?: string; clientId?: number; dateDebut?: string; dateFin?: string } = {}): Promise<PagedList<Devis>> {
    return firstValueFrom(this.http.get<PagedList<Devis>>(`${this.base}/devis`, { params: buildParams({ page: 1, pageSize: 10, ...p }) }));
  }
  mouvementsListPaged(p: { page?: number; pageSize?: number; search?: string; type?: string; source?: string; produitId?: number; dateDebut?: string; dateFin?: string } = {}): Promise<PagedList<MouvementStock>> {
    return firstValueFrom(this.http.get<PagedList<MouvementStock>>(`${this.base}/mouvements-stock`, { params: buildParams({ page: 1, pageSize: 25, ...p }) }));
  }
  auditListPaged(p: { page?: number; pageSize?: number; search?: string; action?: string; entite?: string; sensibleOnly?: boolean } = {}): Promise<PagedList<AuditLog>> {
    return firstValueFrom(this.http.get<PagedList<AuditLog>>(`${this.base}/logs`, { params: buildParams({ page: 1, pageSize: 25, ...p }) }));
  }

  // ── PARAMÈTRES FACTURATION ──
  getFacturationSettings(): Promise<FacturationSettings> {
    return firstValueFrom(this.http.get<FacturationSettings>(`${this.base}/parametres/facturation`));
  }
  updateFacturationSettings(data: FacturationSettings): Promise<FacturationSettings> {
    return firstValueFrom(this.http.put<FacturationSettings>(`${this.base}/parametres/facturation`, data));
  }

  // ── PARAMÈTRES ENTREPRISE ──
  getEntrepriseSettings(): Promise<EntrepriseSettings> {
    return firstValueFrom(this.http.get<EntrepriseSettings>(`${this.base}/parametres/entreprise`));
  }
  updateEntrepriseSettings(data: EntrepriseSettings): Promise<EntrepriseSettings> {
    return firstValueFrom(this.http.put<EntrepriseSettings>(`${this.base}/parametres/entreprise`, data));
  }

  // ── STATS ──
  clientsStats(): Promise<ClientsStats> {
    return firstValueFrom(this.http.get<ClientsStats>(`${this.base}/clients/stats`));
  }
  fournisseursStats(): Promise<FournisseursStats> {
    return firstValueFrom(this.http.get<FournisseursStats>(`${this.base}/fournisseurs/stats`));
  }
  facturesStats(): Promise<FacturesStats> {
    return firstValueFrom(this.http.get<FacturesStats>(`${this.base}/factures/stats`));
  }
  devisStats(): Promise<DevisStats> {
    return firstValueFrom(this.http.get<DevisStats>(`${this.base}/devis/stats`));
  }
  chargesStats(): Promise<ChargesStats> {
    return firstValueFrom(this.http.get<ChargesStats>(`${this.base}/charges/stats`));
  }
  mouvementsStats(): Promise<MouvementsStats> {
    return firstValueFrom(this.http.get<MouvementsStats>(`${this.base}/mouvements-stock/stats`));
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
