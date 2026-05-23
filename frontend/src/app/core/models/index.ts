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

export interface MonthBar  { label: string; ca: number; dep: number; }
export interface DonutItem  { label: string; amount: number; color: string; }
export interface TopClientDash { clientId: number; nomClient: string; initiales: string; totalDepense: number; }
export interface TopProduitDash { produitId: number; nomProduit: string; image?: string; quantiteVendue: number; montantTotal: number; }
export interface StockAlerte { produitId: number; nomProduit: string; reference: string; image?: string; quantiteStock: number; seuilAlerte: number; estRupture: boolean; }

export interface FullDashboard {
  caDuMois: number; caDuJour: number; ventesDuMois: number; ventesDuJour: number;
  totalClients: number; nouveauxClientsDuMois: number;
  totalProduits: number; produitsStockFaible: number; produitsRupture: number;
  montantImpaye: number; nombreFacturesImpayees: number;
  montantImpayeDebit: number; nombreAchatsImpayes: number; nombreChargesImpayees: number;
  achatsDuMois: number; chargesDuMois: number; depensesDuMois: number;
  resultatNet: number; margeRate: number;
  trendRevenu: number; trendDepenses: number;
  tauxEncaissement: number; tauxFidelite: number;
  panierMoyen: number; totalQteVendue: number;
  last6Months: MonthBar[];
  donutItems: DonutItem[];
  topClients: TopClientDash[];
  topProduits: TopProduitDash[];
  stockAlertes: StockAlerte[];
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
  prixVenteHT: number;
  tvaVente: number;
  prixVenteTTC: number;
  quantiteStock: number;
  seuilAlerte: number;
  quantiteReappro: number;
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
  sourceAcquisition?: string;
  delaiPaiement?: number;
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
  remise: number;
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
  remise: number;
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

export interface PaiementAchat {
  id: number;
  achatId: number;
  achatReference: string;
  fournisseurId: number;
  nomFournisseur: string;
  iconeFournisseur?: string;
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
export type FacturationSettings = {
  prefixeFacture: string;
  prefixeVente: string;
  prefixeAchat: string;
  prefixeProduit: string;
  tvaParDefaut: number;
  delaiPaiement: number;
  includeAnnee: boolean;
};

export type Devise = 'MAD' | 'EUR' | 'USD';
export type FormatDate = 'dd/MM/yyyy' | 'MM/dd/yyyy' | 'yyyy-MM-dd';
export type HeureFormat = '24h' | '12h';

export interface AppSettings {
  devise: Devise;
  formatDate: FormatDate;
  timezone: string;
  heureFormat: HeureFormat;
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
    swift?: string;
    logo?: string;
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
    includeAnnee: boolean;
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

// ============ NOTIFICATIONS ============
export interface AppNotification {
  id: number;
  titre: string;
  message: string;
  type: 'Info' | 'Success' | 'Warning' | 'Danger';
  typeLibelle: string;
  categorie: 'Vente' | 'Achat' | 'Charge' | 'Paiement' | 'Stock' | 'Facture' | 'Systeme';
  categorieLibelle: string;
  isRead: boolean;
  entiteId?: number;
  entiteReference?: string;
  lienUrl?: string;
  createdAt: string;
  tempsEcoule: string;
}

export interface NotificationSummary {
  unreadCount: number;
  recent: AppNotification[];
}

// ============ CHARGES ============
export interface CategorieCharge {
  id: number;
  nom: string;
  icone?: string;
  nombreCharges: number;
}

export interface PaiementCharge {
  id: number;
  chargeId: number;
  chargeReference: string;
  montant: number;
  methode: string;
  methodeLibelle: string;
  statut: string;
  statutLibelle: string;
  datePaiement: string;
  reference?: string;
  notes?: string;
}

export interface Charge {
  id: number;
  reference: string;
  titre: string;
  description?: string;
  montant: number;
  montantPaye: number;
  reste: number;
  progressionPaiement: number;
  justificatif?: string;
  statut: string;
  statutLibelle: string;
  dateCharge: string;
  categorieChargeId: number;
  nomCategorie: string;
  iconeCategorie?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  fournisseurId?: number;
  nomFournisseur?: string;
  iconeFournisseur?: string;
  estRecurrente: boolean;
  periodicite?: string;
  dateProchaine?: string;
  paiements: PaiementCharge[];
}

// ============ DEVIS ============
export interface LigneDevis {
  id?: number;
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  quantite: number;
  prixUnitaire: number;
  remise: number;
  tva: number;
  total: number;
}

export interface Devis {
  id: number;
  reference: string;
  clientId: number;
  nomClient: string;
  clientInitiales?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  dateDevis: string;
  dateValidite?: string;
  montantTotalHT: number;
  montantTVA: number;
  montantTotal: number;
  statut: string;
  statutLibelle: string;
  notes?: string;
  venteId?: number;
  venteReference?: string;
  nombreArticles: number;
  estExpire: boolean;
  lignes: LigneDevis[];
}

export interface ConversionDevisResult {
  venteId: number;
  venteReference: string;
  devisReference: string;
}

// ============ COMMANDES ============
export interface LigneCommande {
  id?: number;
  produitId: number;
  nomProduit?: string;
  referenceProduit?: string;
  quantite: number;
  prixUnitaire: number;
  remise: number;
  tva: number;
  total: number;
}

export interface Commande {
  id: number;
  reference: string;
  clientId: number;
  nomClient: string;
  clientInitiales?: string;
  devisId?: number;
  devisReference?: string;
  utilisateurId: number;
  nomUtilisateur: string;
  dateCommande: string;
  dateLivraison?: string;
  montantTotalHT: number;
  montantTVA: number;
  montantTotal: number;
  statut: string;
  statutLibelle: string;
  notes?: string;
  venteId?: number;
  venteReference?: string;
  nombreArticles: number;
  lignes: LigneCommande[];
}

export interface ConversionCommandeResult {
  commandeId: number;
  commandeReference: string;
  devisId?: number;
  devisReference?: string;
}

export interface CommandeStats {
  total: number;
  enAttente: number;
  confirmees: number;
  converties: number;
  montantTotal: number;
}

// ============ RAPPORTS ============
export interface PLMois {
  mois: number;
  nomMois: string;
  revenuHT: number;
  revenuTVA: number;
  revenuTTC: number;
  coutAchat: number;
  chargesOp: number;
  resultatBrut: number;
  resultatNet: number;
}

export interface PLReport {
  annee: number;
  mois: PLMois[];
  totalRevenuHT: number;
  totalRevenuTVA: number;
  totalRevenuTTC: number;
  totalCoutAchat: number;
  totalChargesOp: number;
  totalResultatBrut: number;
  totalResultatNet: number;
  montantImpayeCredit: number;
  nombreFacturesImpayees: number;
  montantImpayeDebit: number;
  nombreAchatsImpayes: number;
  nombreChargesImpayees: number;
}

export interface TVAMois {
  mois: number;
  nomMois: string;
  tvaCollectee: number;
  tvaDeductible: number;
  tvaNette: number;
}

export interface TVAReport {
  annee: number;
  mois: TVAMois[];
  totalTVACollectee: number;
  totalTVADeductible: number;
  totalTVANette: number;
}

// ============ GLOBAL SEARCH ============
export interface SearchItem {
  type: string;
  id: number;
  label: string;
  sublabel?: string;
  badge?: string;
  route: string;
  icon: string;
}

export interface SearchResults {
  clients: SearchItem[];
  produits: SearchItem[];
  ventes: SearchItem[];
  factures: SearchItem[];
  devis: SearchItem[];
  achats: SearchItem[];
  fournisseurs: SearchItem[];
  total: number;
}

// ============ RAPPORTS - Balance Âgée ============
export interface BalanceAgeeClient {
  clientId: number;
  nomClient: string;
  initiales: string;
  totalImpaye: number;
  courant: number;
  j1_30: number;
  j31_60: number;
  j61_90: number;
  j90Plus: number;
}

export interface BalanceAgeeReport {
  dateArrete: string;
  clients: BalanceAgeeClient[];
  totalImpaye: number;
  totalCourant: number;
  totalJ1_30: number;
  totalJ31_60: number;
  totalJ61_90: number;
  totalJ90Plus: number;
  nombreClients: number;
}

// ============ RAPPORTS - Performance Commerciale ============
export interface TopClientPerf {
  clientId: number;
  nomClient: string;
  initiales: string;
  nombreVentes: number;
  montantTotalHT: number;
  montantTotal: number;
  montantPaye: number;
  montantImpaye: number;
  panierMoyen: number;
}

export interface TopProduitPerf {
  produitId: number;
  nomProduit: string;
  reference: string;
  quantiteVendue: number;
  montantHT: number;
  pourcentageCA: number;
}

export interface PerformanceCommerciale {
  annee: number;
  nombreVentes: number;
  nombreClients: number;
  caHt: number;
  caTtc: number;
  topClients: TopClientPerf[];
  topProduits: TopProduitPerf[];
}

// ============ PAGED LIST ============
export interface PagedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ============ STATS DTOs ============
export interface ClientsStats { total: number; actifs: number; ca: number; impayes: number; }
export interface FournisseursStats { total: number; produits: number; commandes: number; achats: number; }
export interface FacturesTabCounts { all: number; payee: number; partiel: number; enAttente: number; enRetard: number; annulee: number; }
export interface FacturesStats { totalMois: number; totalPaye: number; payeePct: number; enAttenteCount: number; enAttenteMontant: number; enRetardCount: number; enRetardMontant: number; tabCounts: FacturesTabCounts; }
export interface DevisStats { total: number; acceptes: number; convertis: number; montantPotentiel: number; tauxAcceptation: number; }
export interface ChargesStats { totalMois: number; count: number; impayes: number; nbImpayes: number; totalGlobal: number; }
export interface MouvementsStats { total: number; entrees: number; sorties: number; }

export type EntrepriseSettings = {
  raisonSociale: string;
  adresse?: string;
  telephone?: string;
  email?: string;
  ice?: string;
  rc?: string;
  if?: string;
  patente?: string;
  cnss?: string;
  capital?: string;
  rib?: string;
  banque?: string;
  swift?: string;
  logo?: string;
};
