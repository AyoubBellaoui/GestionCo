// ============ AUTH ============
export interface User {
  id: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  role: string;
  initiales?: string;
  clientId?: number;
  nomClient?: string;
  isActive: boolean;
  lastLoginAt?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  user: User;
}

// ============ DASHBOARD ============
export interface DashboardStats {
  caDuMois: number;
  caDuJour: number;
  ventesDuMois: number;
  ventesDuJour: number;
  totalClients: number;
  nouveauxClientsDuMois: number;
  totalProduits: number;
  produitsStockFaible: number;
  produitsRupture: number;
  montantImpaye: number;
  nombreFacturesImpayees: number;
}

// ============ PRODUITS ============
export interface Produit {
  id: number;
  reference: string;
  nom: string;
  description?: string;
  image?: string;
  codeBarre?: string;
  prixHT: number;
  tva: number;
  prixTTC: number;
  quantiteStock: number;
  seuilAlerte: number;
  isStockFaible: boolean;
  isRupture: boolean;
  categorieId?: number;
  categorieNom?: string;
  categorieIcone?: string;
  fournisseurId?: number;
  fournisseurNom?: string;
  isActive: boolean;
  createdAt: string;
}

// ============ CATEGORIES ============
export interface Categorie {
  id: number;
  nom: string;
  description?: string;
  icone?: string;
  nombreProduits?: number;
}

// ============ CLIENTS ============
export interface Client {
  id: number;
  nomClient: string;
  type: string;
  ice?: string;
  rc?: string;
  if?: string;
  adresse?: string;
  ville?: string;
  telephone?: string;
  email?: string;
  personneContact?: string;
  isActive: boolean;
  initiales: string;
  nombreCommandes: number;
  totalDepense: number;
  totalImpaye: number;
  createdAt: string;
  utilisateurId?: number;
}

// ============ FOURNISSEURS ============
export interface Fournisseur {
  id: number;
  nom: string;
  icone?: string;
  telephone?: string;
  email?: string;
  adresse?: string;
  siteWeb?: string;
  personneContact?: string;
  isActive: boolean;
  nombreProduits: number;
  nombreAchats: number;
  montantTotalAchats: number;
  createdAt: string;
}

// ============ VENTES ============
export interface LigneVente {
  id?: number;
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  imageProduit?: string;
  quantite: number;
  prixUnitaire: number;
  tva: number;
  total: number;
}

export interface Vente {
  id: number;
  reference: string;
  clientId: number;
  nomClient: string;
  clientICE?: string;
  clientInitiales?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  dateVente: string;
  dateEcheance?: string;
  montantTotalHT: number;
  montantTVA: number;
  montantTotal: number;
  montantPaye: number;
  reste: number;
  progressionPaiement: number;
  statut: string;
  statutLibelle: string;
  nombreArticles: number;
  lignes: LigneVente[];
  numeroFacture?: string;
  factureId?: number;
}

// ============ ACHATS ============
export interface LigneAchat {
  id?: number;
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  quantite: number;
  prixUnitaire: number;
  total: number;
}

export interface Achat {
  id: number;
  reference: string;
  fournisseurId: number;
  nomFournisseur: string;
  iconeFournisseur?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  dateAchat: string;
  montantTotal: number;
  montantPaye: number;
  reste: number;
  progressionPaiement: number;
  statut: string;
  statutLibelle: string;
  notes?: string;
  nombreArticles: number;
  lignes: LigneAchat[];
}

// ============ PAIEMENTS ============
export interface Paiement {
  id: number;
  venteId: number;
  venteReference: string;
  clientId: number;
  nomClient: string;
  clientInitiales?: string;
  montant: number;
  methode: string;
  methodeLibelle: string;
  statut: string;
  statutLibelle: string;
  datePaiement: string;
  reference?: string;
  notes?: string;
}

// ============ FACTURES ============
export interface Facture {
  id: number;
  numeroFacture: string;
  venteId: number;
  venteReference: string;
  clientId: number;
  nomClient: string;
  clientICE?: string;
  clientInitiales?: string;
  dateEmission: string;
  dateEcheance: string;
  montantTotal: number;
  montantPaye: number;
  statut: string;
  statutLibelle: string;
  estEnvoyeeEmail: boolean;
  dateEnvoiEmail?: string;
  joursEcheance: number;
  estEnRetard: boolean;
}

// ============ MOUVEMENTS STOCK ============
export interface MouvementStock {
  id: number;
  produitId: number;
  nomProduit: string;
  referenceProduit: string;
  imageProduit?: string;
  type: string;
  quantite: number;
  stockAvant: number;
  stockApres: number;
  source: string;
  sourceLibelle: string;
  referenceText?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  raison?: string;
  commentaire?: string;
  dateMouvement: string;
}

// ============ AUDIT LOGS ============
export interface AuditLog {
  id: number;
  utilisateurId?: number;
  nomUtilisateur: string;
  initialesUtilisateur?: string;
  action: string;
  actionLibelle: string;
  entite: string;
  entiteReference?: string;
  entiteId?: number;
  description: string;
  anciennesValeurs?: string;
  nouvellesValeurs?: string;
  ipAddress?: string;
  userAgent?: string;
  estSensible: boolean;
  dateAction: string;
}

// ============ UTILISATEURS ============
export interface UtilisateurDto {
  id: number;
  nom: string;
  prenom: string;
  email: string;
  telephone?: string;
  role: string;
  roleLibelle: string;
  isActive: boolean;
  lastLoginAt?: string;
  nomComplet: string;
  initiales: string;
}

export interface CreateUtilisateurPayload {
  nom: string;
  prenom: string;
  email: string;
  password: string;
  role: string;
  telephone?: string;
}

export interface UpdateUtilisateurPayload {
  nom: string;
  prenom: string;
  telephone?: string;
  role: string;
  isActive: boolean;
}

// ============ SETTINGS ============
export type Devise = 'MAD' | 'EUR' | 'USD';
export type FormatDate = 'dd/MM/yyyy' | 'MM/dd/yyyy' | 'yyyy-MM-dd';

export interface AppSettings {
  devise: Devise;
  formatDate: FormatDate;
  entreprise: {
    raisonSociale: string;
    adresse: string;
    telephone: string;
    email: string;
    ice: string;
    rc: string;
    if: string;
    patente: string;
    cnss: string;
    capital: string;
    rib: string;
    banque: string;
  };
  notifs: {
    nouvelleVente: boolean;
    stockFaible: boolean;
    factureImpayee: boolean;
    nouveauClient: boolean;
    emailDaily: boolean;
    emailWeekly: boolean;
  };
  facturation: {
    prefixeFacture: string;
    prefixeVente: string;
    prefixeAchat: string;
    prefixeProduit: string;
    tvaParDefaut: number;
    delaiPaiement: number;
  };
}

// ============ VENTES SANS FACTURE ============
export interface VenteSansFacture {
  id: number;
  reference: string;
  dateVente: string;
  nomClient: string;
  montantTotal: number;
}

// ============ PAGED LIST ============
export interface PagedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
