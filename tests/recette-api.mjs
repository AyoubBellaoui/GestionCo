// Recette automatisée de l'API GestionCo (Node 18+, aucune dépendance).
// Usage : node tests/recette-api.mjs [baseUrl] [rapport.json]
// À exécuter sur une base de test vierge (ex. GestionCoDb_Recette) : le script crée des données.

import fs from 'node:fs';

const BASE = process.argv[2] ?? 'http://localhost:5000';
const OUT = process.argv[3] ?? 'recette-api-resultats.json';
const YEAR = new Date().getFullYear();
const ADMIN = { email: 'admin@gestionco.ma', password: 'Admin123!' };

const results = [];
const timings = [];
const ctx = {};

async function api(method, path, { token, body, raw } = {}) {
  const headers = {};
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const t0 = performance.now();
  const res = await fetch(BASE + path, { method, headers, body: body !== undefined ? JSON.stringify(body) : undefined });
  const type = res.headers.get('content-type') ?? '';
  timings.push({ method, path: path.split('?')[0], ms: performance.now() - t0, pdf: type.includes('pdf') });
  let data = null;
  if (raw || type.includes('application/pdf')) data = Buffer.from(await res.arrayBuffer());
  else { const txt = await res.text(); try { data = txt ? JSON.parse(txt) : null; } catch { data = txt; } }
  return { status: res.status, type, data };
}

async function test(id, module, title, expected, fn) {
  let ok = false, obtained = '';
  try {
    const r = await fn();
    ok = r.ok; obtained = r.obtained;
  } catch (e) { ok = false; obtained = 'Exception : ' + e.message; }
  results.push({ id, module, title, expected, obtained, ok });
  console.log(`${ok ? 'OK  ' : 'ECHEC'} ${id.padEnd(6)} ${title} — ${obtained}`);
}
const near = (a, b) => Math.abs(Number(a) - Number(b)) < 0.01;
const items = d => d?.items ?? d?.Items ?? d;

// ───────────────────────── Authentification ─────────────────────────
await test('AUT-01', 'Authentification', 'Connexion avec des identifiants valides', '200 + jeton d\'accès et jeton de rafraîchissement', async () => {
  const r = await api('POST', '/api/auth/login', { body: ADMIN });
  ctx.admin = r.data?.accessToken; ctx.adminRefresh = r.data?.refreshToken;
  return { ok: r.status === 200 && !!ctx.admin && !!ctx.adminRefresh, obtained: `${r.status}, jetons ${ctx.admin ? 'reçus' : 'absents'}` };
});
await test('AUT-02', 'Authentification', 'Connexion avec un mot de passe erroné', '401', async () => {
  const r = await api('POST', '/api/auth/login', { body: { email: ADMIN.email, password: 'mauvais' } });
  return { ok: r.status === 401, obtained: String(r.status) };
});
await test('AUT-03', 'Authentification', 'Accès à une ressource protégée sans jeton', '401', async () => {
  const r = await api('GET', '/api/produits');
  return { ok: r.status === 401, obtained: String(r.status) };
});
await test('AUT-04', 'Authentification', 'Profil de l\'utilisateur connecté (/me)', '200 + e-mail de l\'administrateur', async () => {
  const r = await api('GET', '/api/auth/me', { token: ctx.admin });
  return { ok: r.status === 200 && r.data?.email === ADMIN.email, obtained: `${r.status}, ${r.data?.email}` };
});
await test('AUT-05', 'Authentification', 'Rafraîchissement du jeton (rotation)', '200 + nouveau jeton de rafraîchissement', async () => {
  const r = await api('POST', '/api/auth/refresh', { body: { accessToken: ctx.admin, refreshToken: ctx.adminRefresh } });
  const ok = r.status === 200 && r.data?.refreshToken && r.data.refreshToken !== ctx.adminRefresh;
  ctx.oldRefresh = ctx.adminRefresh;
  if (r.data?.accessToken) { ctx.admin = r.data.accessToken; ctx.adminRefresh = r.data.refreshToken; }
  return { ok, obtained: `${r.status}, jeton ${ok ? 'renouvelé' : 'inchangé'}` };
});
await test('AUT-06', 'Authentification', 'Réutilisation d\'un ancien jeton de rafraîchissement', '401', async () => {
  const r = await api('POST', '/api/auth/refresh', { body: { accessToken: ctx.admin, refreshToken: ctx.oldRefresh } });
  return { ok: r.status === 401, obtained: String(r.status) };
});
await test('AUT-07', 'Authentification', 'Jeton JWT falsifié', '401', async () => {
  const forged = ctx.admin.slice(0, -4) + (ctx.admin.endsWith('AAAA') ? 'BBBB' : 'AAAA');
  const r = await api('GET', '/api/produits', { token: forged });
  return { ok: r.status === 401, obtained: String(r.status) };
});

// ───────────────────────── Rôles ─────────────────────────
await test('ROL-01', 'Droits d\'accès', 'L\'administrateur crée un compte Gestionnaire', '200', async () => {
  const r = await api('POST', '/api/utilisateurs', { token: ctx.admin, body: { nom: 'Recette', prenom: 'Gestionnaire', email: 'gestionnaire@recette.ma', password: 'Gestion123!', role: 'Gestionnaire' } });
  return { ok: r.status === 200, obtained: String(r.status) };
});
await test('ROL-02', 'Droits d\'accès', 'Connexion du Gestionnaire', '200 + rôle Gestionnaire', async () => {
  const r = await api('POST', '/api/auth/login', { body: { email: 'gestionnaire@recette.ma', password: 'Gestion123!' } });
  ctx.gest = r.data?.accessToken;
  return { ok: r.status === 200 && r.data?.user?.role === 'Gestionnaire', obtained: `${r.status}, rôle ${r.data?.user?.role}` };
});
for (const [id, title, method, path] of [
  ['ROL-03', 'Le Gestionnaire consulte le journal d\'audit', 'GET', '/api/logs'],
  ['ROL-04', 'Le Gestionnaire consulte les comptes utilisateurs', 'GET', '/api/utilisateurs'],
  ['ROL-05', 'Le Gestionnaire consulte la configuration SMTP', 'GET', '/api/parametres/smtp'],
]) {
  await test(id, 'Droits d\'accès', title, '403', async () => {
    const r = await api(method, path, { token: ctx.gest });
    return { ok: r.status === 403, obtained: String(r.status) };
  });
}

// ───────────────────────── Catalogue ─────────────────────────
await test('CAT-01', 'Catalogue', 'Création d\'une catégorie', '200', async () => {
  const r = await api('POST', '/api/categories', { token: ctx.gest, body: { nom: 'Recette Informatique', description: 'Tests', icone: '💻' } });
  ctx.cat = r.data?.id;
  return { ok: r.status === 200 && !!ctx.cat, obtained: String(r.status) };
});
await test('CAT-02', 'Catalogue', 'Création d\'un fournisseur', '200', async () => {
  const r = await api('POST', '/api/fournisseurs', { token: ctx.gest, body: { nom: 'Fournisseur Recette SARL', telephone: '0522000000', email: 'contact@fournisseur-recette.ma' } });
  ctx.four = r.data?.id;
  return { ok: r.status === 200 && !!ctx.four, obtained: String(r.status) };
});
await test('CAT-03', 'Catalogue', 'Création d\'un produit (référence automatique)', `200 + référence PRD-${YEAR}-NNNN`, async () => {
  const r = await api('POST', '/api/produits', { token: ctx.gest, body: { nom: 'Ordinateur portable Recette', prixHT: 6000, tva: 20, prixVenteHT: 8000, tvaVente: 20, quantiteStock: 10, seuilAlerte: 3, categorieId: ctx.cat, fournisseurId: ctx.four } });
  ctx.p1 = r.data?.id;
  const ref = r.data?.reference ?? '';
  return { ok: r.status === 200 && new RegExp(`^PRD-${YEAR}-\\d{4}$`).test(ref), obtained: `${r.status}, ${ref}` };
});
await test('CAT-04', 'Catalogue', 'Création d\'un second produit', '200', async () => {
  const r = await api('POST', '/api/produits', { token: ctx.gest, body: { nom: 'Écran 27 pouces Recette', prixHT: 1500, tva: 20, prixVenteHT: 2000, tvaVente: 20, quantiteStock: 5, seuilAlerte: 2, categorieId: ctx.cat, fournisseurId: ctx.four } });
  ctx.p2 = r.data?.id;
  return { ok: r.status === 200 && !!ctx.p2, obtained: String(r.status) };
});
await test('CAT-05', 'Catalogue', 'Produit sans nom (validation)', '400', async () => {
  const r = await api('POST', '/api/produits', { token: ctx.gest, body: { nom: '', prixHT: 10, prixVenteHT: 20, quantiteStock: 1 } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('CAT-06', 'Catalogue', 'Produit avec un prix négatif (validation)', '400', async () => {
  const r = await api('POST', '/api/produits', { token: ctx.gest, body: { nom: 'Prix négatif', prixHT: -5, prixVenteHT: 20, quantiteStock: 1 } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('CAT-07', 'Catalogue', 'Liste paginée côté serveur (pageSize = 1)', '200 + 1 élément, total ≥ 2', async () => {
  const r = await api('GET', '/api/produits?page=1&pageSize=1', { token: ctx.gest });
  const n = items(r.data)?.length, total = r.data?.totalCount;
  return { ok: r.status === 200 && n === 1 && total >= 2, obtained: `${r.status}, ${n} élément(s) / total ${total}` };
});
await test('CAT-08', 'Catalogue', 'Recherche d\'un produit par son nom', '200 + produit trouvé', async () => {
  const r = await api('GET', '/api/produits?search=Recette&page=1&pageSize=10', { token: ctx.gest });
  const found = (items(r.data) ?? []).some(p => p.id === ctx.p1);
  return { ok: r.status === 200 && found, obtained: `${r.status}, ${found ? 'trouvé' : 'non trouvé'}` };
});
await test('CAT-09', 'Catalogue', 'Suppression d\'une catégorie contenant des produits', '400 (refus métier)', async () => {
  const r = await api('DELETE', `/api/categories/${ctx.cat}`, { token: ctx.admin });
  return { ok: r.status === 400, obtained: `${r.status}${r.data?.message ? ' — ' + r.data.message : ''}` };
});
await test('ROL-06', 'Droits d\'accès', 'Le Gestionnaire supprime un produit', '403', async () => {
  const r = await api('DELETE', `/api/produits/${ctx.p2}`, { token: ctx.gest });
  return { ok: r.status === 403, obtained: String(r.status) };
});

// ───────────────────────── Clients ─────────────────────────
await test('CLI-01', 'Clients', 'Création d\'un client société (ICE à 15 chiffres)', '200', async () => {
  const r = await api('POST', '/api/clients', { token: ctx.gest, body: { nomClient: 'Société Recette SARL', type: 'Entreprise', ice: '001234567000089', ville: 'Casablanca', email: 'achat@societe-recette.ma', delaiPaiement: 30 } });
  ctx.client = r.data?.id;
  return { ok: r.status === 200 && !!ctx.client, obtained: String(r.status) };
});
await test('CLI-02', 'Clients', 'Client société avec un ICE invalide (validation)', '400', async () => {
  const r = await api('POST', '/api/clients', { token: ctx.gest, body: { nomClient: 'ICE invalide', type: 'Entreprise', ice: '12AB' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('CLI-03', 'Clients', 'Client sans nom (validation)', '400', async () => {
  const r = await api('POST', '/api/clients', { token: ctx.gest, body: { nomClient: '', type: 'Particulier' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});

// ───────────────────────── Devis ─────────────────────────
const lignes = () => [
  { produitId: ctx.p1, quantite: 2, prixUnitaire: 8000, remise: 10, tva: 20 },
  { produitId: ctx.p2, quantite: 1, prixUnitaire: 2000, remise: 0, tva: 20 },
];
// HT = 2×8000×0,9 + 2000 = 16 400 ; TVA = 3 280 ; TTC = 19 680
await test('DEV-01', 'Devis', 'Création d\'un devis de deux lignes (calcul HT / TVA / TTC)', `200, Brouillon, DVS-${YEAR}-NNNN, HT 16 400 / TTC 19 680`, async () => {
  const r = await api('POST', '/api/devis', { token: ctx.gest, body: { clientId: ctx.client, lignes: lignes(), notes: 'Devis de recette' } });
  ctx.devis = r.data?.id;
  const d = r.data ?? {};
  const ht = d.montantTotalHT ?? d.montantHT, ttc = d.montantTotal ?? d.montantTTC;
  const ok = r.status === 200 && d.statut === 'Brouillon' && new RegExp(`^DVS-${YEAR}-\\d{4}$`).test(d.reference) && near(ht, 16400) && near(ttc, 19680);
  return { ok, obtained: `${r.status}, ${d.statut}, ${d.reference}, HT ${ht} / TTC ${ttc}` };
});
await test('DEV-02', 'Devis', 'Conversion d\'un devis non accepté en commande', '400', async () => {
  const r = await api('POST', `/api/devis/${ctx.devis}/convertir-commande`, { token: ctx.gest });
  return { ok: r.status === 400, obtained: `${r.status}${r.data?.message ? ' — ' + r.data.message : ''}` };
});
await test('DEV-03', 'Devis', 'Transition interdite Brouillon → Converti', '400', async () => {
  const r = await api('PUT', `/api/devis/${ctx.devis}/statut`, { token: ctx.gest, body: { statut: 'Converti' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('DEV-04', 'Devis', 'Transitions Brouillon → Envoyé → Accepté', '200 puis 200', async () => {
  const a = await api('PUT', `/api/devis/${ctx.devis}/statut`, { token: ctx.gest, body: { statut: 'Envoye' } });
  const b = await api('PUT', `/api/devis/${ctx.devis}/statut`, { token: ctx.gest, body: { statut: 'Accepte' } });
  return { ok: a.status === 200 && b.status === 200, obtained: `${a.status} puis ${b.status}` };
});
await test('DEV-05', 'Devis', 'Modification d\'un devis accepté', '400 (seul un brouillon est modifiable)', async () => {
  const r = await api('PUT', `/api/devis/${ctx.devis}`, { token: ctx.gest, body: { clientId: ctx.client, lignes: lignes() } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('DEV-06', 'Devis', 'Conversion du devis accepté en commande', `200 + CMD-${YEAR}-NNNN, devis « Converti »`, async () => {
  const r = await api('POST', `/api/devis/${ctx.devis}/convertir-commande`, { token: ctx.gest });
  ctx.cmd = r.data?.commandeId;
  const d = await api('GET', `/api/devis/${ctx.devis}`, { token: ctx.gest });
  const ok = r.status === 200 && new RegExp(`^CMD-${YEAR}-\\d{4}$`).test(r.data?.commandeReference ?? '') && d.data?.statut === 'Converti';
  return { ok, obtained: `${r.status}, ${r.data?.commandeReference}, devis ${d.data?.statut}` };
});
await test('DEV-07', 'Devis', 'Seconde conversion du même devis', '400', async () => {
  const r = await api('POST', `/api/devis/${ctx.devis}/convertir-commande`, { token: ctx.gest });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('DEV-08', 'Devis', 'Génération du PDF du devis', '200 + application/pdf', async () => {
  const r = await api('POST', `/api/devis/${ctx.devis}/pdf`, { token: ctx.gest, body: {}, raw: true });
  const ok = r.status === 200 && r.type.includes('pdf') && r.data.subarray(0, 4).toString() === '%PDF';
  return { ok, obtained: `${r.status}, ${r.type}, ${r.data?.length ?? 0} octets` };
});

// ───────────────────────── Commandes ─────────────────────────
await test('CMD-01', 'Commandes', 'Commande issue du devis : statut et lignes reprises', 'Brouillon, 2 lignes, devis d\'origine renseigné', async () => {
  const r = await api('GET', `/api/commandes/${ctx.cmd}`, { token: ctx.gest });
  const d = r.data ?? {};
  const ok = r.status === 200 && d.statut === 'Brouillon' && (d.lignes?.length === 2) && d.devisId === ctx.devis;
  return { ok, obtained: `${r.status}, ${d.statut}, ${d.lignes?.length} lignes, devisId ${d.devisId}` };
});
await test('CMD-02', 'Commandes', 'Suivi de livraison sur une commande non confirmée', '400', async () => {
  const r = await api('PUT', `/api/commandes/${ctx.cmd}/etat-livraison`, { token: ctx.gest, body: { etatLivraison: 'EnPreparation' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('CMD-03', 'Commandes', 'Confirmation de la commande', '200', async () => {
  const r = await api('PUT', `/api/commandes/${ctx.cmd}/statut`, { token: ctx.gest, body: { statut: 'Confirmee' } });
  return { ok: r.status === 200, obtained: String(r.status) };
});
await test('CMD-04', 'Commandes', 'Saut d\'étape de livraison (Non commencé → En livraison)', '400', async () => {
  const r = await api('PUT', `/api/commandes/${ctx.cmd}/etat-livraison`, { token: ctx.gest, body: { etatLivraison: 'EnLivraison' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('CMD-05', 'Commandes', 'Suivi de livraison complet (préparation → livraison → livrée)', '200 × 3', async () => {
  const s = [];
  for (const e of ['EnPreparation', 'EnLivraison', 'Livre']) s.push((await api('PUT', `/api/commandes/${ctx.cmd}/etat-livraison`, { token: ctx.gest, body: { etatLivraison: e } })).status);
  return { ok: s.every(x => x === 200), obtained: s.join(', ') };
});

// ───────────────────────── Ventes, stock, facture ─────────────────────────
await test('VEN-01', 'Ventes', 'Conversion de la commande en vente', '200, commande « Convertie »', async () => {
  const r = await api('POST', '/api/ventes', { token: ctx.gest, body: { clientId: ctx.client, commandeId: ctx.cmd, lignes: lignes() } });
  ctx.vente = r.data?.id; ctx.venteData = r.data;
  const c = await api('GET', `/api/commandes/${ctx.cmd}`, { token: ctx.gest });
  return { ok: r.status === 200 && c.data?.statut === 'Convertie' && c.data?.venteId === ctx.vente, obtained: `${r.status}, ${r.data?.reference}, commande ${c.data?.statut}` };
});
await test('VEN-02', 'Ventes', 'Seconde conversion de la même commande (double clic)', '400', async () => {
  const r = await api('POST', '/api/ventes', { token: ctx.gest, body: { clientId: ctx.client, commandeId: ctx.cmd, lignes: lignes() } });
  return { ok: r.status === 400, obtained: `${r.status}${r.data?.message ? ' — ' + r.data.message : ''}` };
});
await test('VEN-03', 'Ventes', 'Montants de la vente (remise et TVA par ligne)', 'HT 16 400 ; TVA 3 280 ; TTC 19 680', async () => {
  const v = ctx.venteData ?? {};
  return { ok: near(v.montantTotalHT, 16400) && near(v.montantTVA, 3280) && near(v.montantTotal, 19680), obtained: `HT ${v.montantTotalHT} ; TVA ${v.montantTVA} ; TTC ${v.montantTotal}` };
});
await test('VEN-04', 'Stock', 'Décrémentation du stock après la vente', 'Produit 1 : 10 → 8 ; produit 2 : 5 → 4', async () => {
  const a = await api('GET', `/api/produits/${ctx.p1}`, { token: ctx.gest });
  const b = await api('GET', `/api/produits/${ctx.p2}`, { token: ctx.gest });
  return { ok: a.data?.quantiteStock === 8 && b.data?.quantiteStock === 4, obtained: `${a.data?.quantiteStock} et ${b.data?.quantiteStock}` };
});
await test('VEN-05', 'Stock', 'Mouvement de stock « Sortie » tracé avec la référence de la vente', 'Mouvement Sortie, stock avant 10 / après 8', async () => {
  const r = await api('GET', `/api/mouvements-stock?produitId=${ctx.p1}&page=1&pageSize=20`, { token: ctx.gest });
  const m = (items(r.data) ?? []).find(x => x.type === 'Sortie' && (x.referenceText === ctx.venteData?.reference || x.reference === ctx.venteData?.reference));
  return { ok: !!m && m.stockAvant === 10 && m.stockApres === 8, obtained: m ? `Sortie ${m.stockAvant} → ${m.stockApres} (${m.referenceText ?? m.reference})` : 'mouvement non trouvé' };
});
await test('VEN-06', 'Facturation', 'Facture générée automatiquement avec la vente', `Numéro FAC-${YEAR}-NNN, statut « En attente »`, async () => {
  const f = ctx.venteData?.facture ?? {};
  ctx.facture = f.id ?? ctx.venteData?.factureId;
  const num = f.numeroFacture ?? ctx.venteData?.numeroFacture ?? '';
  let statut = f.statut;
  if (ctx.facture && !statut) statut = (await api('GET', `/api/factures/${ctx.facture}`, { token: ctx.gest })).data?.statut;
  return { ok: new RegExp(`^FAC-${YEAR}-\\d{3}$`).test(num) && statut === 'EnAttente', obtained: `${num}, ${statut}` };
});
await test('VEN-07', 'Stock', 'Vente d\'une quantité supérieure au stock', '400, stock inchangé', async () => {
  const r = await api('POST', '/api/ventes', { token: ctx.gest, body: { clientId: ctx.client, lignes: [{ produitId: ctx.p2, quantite: 50, prixUnitaire: 2000, tva: 20 }] } });
  const b = await api('GET', `/api/produits/${ctx.p2}`, { token: ctx.gest });
  return { ok: r.status === 400 && b.data?.quantiteStock === 4, obtained: `${r.status}, stock ${b.data?.quantiteStock}` };
});
await test('VEN-08', 'Ventes', 'Vente sans aucune ligne (validation)', '400', async () => {
  const r = await api('POST', '/api/ventes', { token: ctx.gest, body: { clientId: ctx.client, lignes: [] } });
  return { ok: r.status === 400, obtained: String(r.status) };
});
await test('FAC-01', 'Facturation', 'Seconde facture pour la même vente', '400', async () => {
  const r = await api('POST', '/api/factures', { token: ctx.gest, body: { venteId: ctx.vente } });
  return { ok: r.status === 400, obtained: `${r.status}${r.data?.message ? ' — ' + r.data.message : ''}` };
});
await test('FAC-02', 'Facturation', 'Génération du PDF de la facture (QuestPDF)', '200 + application/pdf', async () => {
  const r = await api('POST', `/api/factures/${ctx.facture}/pdf`, { token: ctx.gest, body: {}, raw: true });
  const ok = r.status === 200 && r.type.includes('pdf') && r.data.subarray(0, 4).toString() === '%PDF';
  if (ok) fs.writeFileSync(OUT.replace(/\.json$/, '') + '-facture.pdf', r.data);
  return { ok, obtained: `${r.status}, ${r.type}, ${r.data?.length ?? 0} octets` };
});

// ───────────────────────── Paiements ─────────────────────────
await test('PAI-01', 'Paiements', 'Paiement partiel de 5 000 MAD', '200, facture « Partiellement payée »', async () => {
  const r = await api('POST', `/api/ventes/${ctx.vente}/paiements`, { token: ctx.gest, body: { montant: 5000, methode: 'Virement' } });
  const f = await api('GET', `/api/factures/${ctx.facture}`, { token: ctx.gest });
  return { ok: r.status === 200 && f.data?.statut === 'PartiellementPayee', obtained: `${r.status}, facture ${f.data?.statut}` };
});
await test('PAI-02', 'Paiements', 'Paiement supérieur au reste dû', '400', async () => {
  const r = await api('POST', '/api/paiements', { token: ctx.gest, body: { venteId: ctx.vente, montant: 999999, methode: 'Espece' } });
  return { ok: r.status === 400, obtained: `${r.status}${r.data?.message ? ' — ' + r.data.message : ''}` };
});
await test('PAI-03', 'Paiements', 'Règlement du solde (14 680 MAD)', '200, vente « Payée », facture « Payée »', async () => {
  const r = await api('POST', '/api/paiements', { token: ctx.gest, body: { venteId: ctx.vente, montant: 14680, methode: 'Cheque' } });
  const v = await api('GET', `/api/ventes/${ctx.vente}`, { token: ctx.gest });
  const f = await api('GET', `/api/factures/${ctx.facture}`, { token: ctx.gest });
  return { ok: r.status === 200 && v.data?.statut === 'Paye' && f.data?.statut === 'Payee', obtained: `${r.status}, vente ${v.data?.statut}, facture ${f.data?.statut}` };
});

// ───────────────────────── Achats ─────────────────────────
await test('ACH-01', 'Achats', 'Achat fournisseur : incrémentation du stock', `200, ACH-${YEAR}-NNNN, stock 8 → 13`, async () => {
  const r = await api('POST', '/api/achats', { token: ctx.gest, body: { fournisseurId: ctx.four, lignes: [{ produitId: ctx.p1, quantite: 5, prixUnitaire: 6000 }] } });
  ctx.achat = r.data?.id;
  const a = await api('GET', `/api/produits/${ctx.p1}`, { token: ctx.gest });
  return { ok: r.status === 200 && new RegExp(`^ACH-${YEAR}-\\d{4}$`).test(r.data?.reference ?? '') && a.data?.quantiteStock === 13, obtained: `${r.status}, ${r.data?.reference}, stock ${a.data?.quantiteStock}` };
});
await test('ROL-07', 'Droits d\'accès', 'Le Gestionnaire annule un achat', '403', async () => {
  const r = await api('POST', `/api/achats/${ctx.achat}/cancel`, { token: ctx.gest, body: { raison: 'Test' } });
  return { ok: r.status === 403, obtained: String(r.status) };
});
await test('ACH-02', 'Achats', 'Annulation de l\'achat par l\'administrateur', '200, stock restauré 13 → 8', async () => {
  const r = await api('POST', `/api/achats/${ctx.achat}/cancel`, { token: ctx.admin, body: { raison: 'Recette' } });
  const a = await api('GET', `/api/produits/${ctx.p1}`, { token: ctx.gest });
  return { ok: (r.status === 200 || r.status === 204) && a.data?.quantiteStock === 8, obtained: `${r.status}, stock ${a.data?.quantiteStock}` };
});
await test('ACH-03', 'Achats', 'Seconde annulation du même achat', '400', async () => {
  const r = await api('POST', `/api/achats/${ctx.achat}/cancel`, { token: ctx.admin, body: { raison: 'Recette' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});

// ───────────────────────── Annulation de vente ─────────────────────────
await test('VEN-09', 'Ventes', 'Vente directe puis annulation par l\'administrateur', '200, stock restauré, facture « Annulée »', async () => {
  const c = await api('POST', '/api/ventes', { token: ctx.gest, body: { clientId: ctx.client, lignes: [{ produitId: ctx.p2, quantite: 1, prixUnitaire: 2000, tva: 20 }] } });
  ctx.vente2 = c.data?.id; const fid = c.data?.factureId ?? c.data?.facture?.id;
  const before = (await api('GET', `/api/produits/${ctx.p2}`, { token: ctx.gest })).data?.quantiteStock;
  const r = await api('POST', `/api/ventes/${ctx.vente2}/cancel`, { token: ctx.admin, body: { raison: 'Recette' } });
  const after = (await api('GET', `/api/produits/${ctx.p2}`, { token: ctx.gest })).data?.quantiteStock;
  const f = fid ? (await api('GET', `/api/factures/${fid}`, { token: ctx.gest })).data?.statut : '?';
  return { ok: (r.status === 200 || r.status === 204) && after === before + 1 && f === 'Annulee', obtained: `${r.status}, stock ${before} → ${after}, facture ${f}` };
});
await test('VEN-10', 'Paiements', 'Paiement sur une vente annulée', '400', async () => {
  const r = await api('POST', '/api/paiements', { token: ctx.gest, body: { venteId: ctx.vente2, montant: 100, methode: 'Espece' } });
  return { ok: r.status === 400, obtained: String(r.status) };
});

// ───────────────────────── Charges ─────────────────────────
await test('CHG-01', 'Charges', 'Création d\'une charge avec paiement initial', `200, CHG-${YEAR}-NNNN`, async () => {
  const cats = await api('GET', '/api/categories-charge', { token: ctx.gest });
  const catId = (Array.isArray(cats.data) ? cats.data : items(cats.data) ?? [])[0]?.id;
  const r = await api('POST', '/api/charges', { token: ctx.gest, body: { titre: 'Loyer recette', montant: 3000, categorieChargeId: catId, paiementInitial: 1000, methodePaiementInitial: 'Virement' } });
  ctx.charge = r.data?.id;
  return { ok: r.status === 200 && new RegExp(`^CHG-${YEAR}-\\d{4}$`).test(r.data?.reference ?? ''), obtained: `${r.status}, ${r.data?.reference}` };
});
await test('CHG-02', 'Charges', 'Suppression d\'une charge ayant des paiements', '400', async () => {
  const r = await api('DELETE', `/api/charges/${ctx.charge}`, { token: ctx.admin });
  return { ok: r.status === 400, obtained: String(r.status) };
});

// ───────────────────────── Rapports, tableau de bord, audit ─────────────────────────
await test('RAP-01', 'Rapports', `Compte de résultat ${YEAR}`, '200 + détail sur 12 mois', async () => {
  const r = await api('GET', `/api/reports/pl/${YEAR}`, { token: ctx.gest });
  const months = r.data?.mois ?? r.data?.details ?? r.data?.detailMensuel ?? r.data?.months;
  return { ok: r.status === 200 && Array.isArray(months) && months.length === 12, obtained: `${r.status}, ${Array.isArray(months) ? months.length + ' mois' : 'structure : ' + Object.keys(r.data ?? {}).join(',')}` };
});
await test('RAP-02', 'Rapports', `Déclaration de TVA ${YEAR}`, '200', async () => {
  const r = await api('GET', `/api/reports/tva/${YEAR}`, { token: ctx.gest });
  return { ok: r.status === 200, obtained: String(r.status) };
});
await test('RAP-03', 'Rapports', 'Export PDF du compte de résultat', '200 + application/pdf', async () => {
  const r = await api('GET', `/api/reports/pl/${YEAR}/pdf`, { token: ctx.gest, raw: true });
  return { ok: r.status === 200 && r.type.includes('pdf'), obtained: `${r.status}, ${r.type}` };
});
await test('RAP-04', 'Rapports', 'Balance âgée des créances', '200', async () => {
  const r = await api('GET', '/api/reports/balance-agee', { token: ctx.gest });
  return { ok: r.status === 200, obtained: String(r.status) };
});
await test('DSH-01', 'Tableau de bord', 'Indicateurs agrégés du tableau de bord', '200', async () => {
  const r = await api('GET', '/api/dashboard/full', { token: ctx.gest });
  return { ok: r.status === 200, obtained: String(r.status) };
});
await test('NOT-01', 'Notifications', 'Notification « Nouvelle vente » créée', '200 + notification présente', async () => {
  const r = await api('GET', '/api/notifications?limit=50', { token: ctx.gest });
  const list = r.data?.recent ?? [];
  const found = (Array.isArray(list) ? list : []).some(n => (n.titre ?? '').startsWith('Nouvelle vente'));
  return { ok: r.status === 200 && found, obtained: `${r.status}, ${found ? 'présente' : 'absente'}` };
});
await test('LOG-01', 'Journal d\'audit', 'Actions tracées dans le journal d\'audit (administrateur)', '200 + création de la vente journalisée', async () => {
  const r = await api('GET', '/api/logs?page=1&pageSize=100', { token: ctx.admin });
  const list = items(r.data) ?? [];
  const found = list.some(l => (l.description ?? '').includes(ctx.venteData?.reference ?? '¤'));
  return { ok: r.status === 200 && found, obtained: `${r.status}, ${list.length} entrées, vente ${found ? 'tracée' : 'non tracée'}` };
});

// ───────────────────────── Sécurité : limitation des échecs de connexion ─────────────────────────
await test('SEC-01', 'Sécurité', '6e tentative après 5 échecs de connexion depuis la même adresse IP', '429 (blocage 15 minutes)', async () => {
  const codes = [];
  for (let i = 0; i < 5; i++) codes.push((await api('POST', '/api/auth/login', { body: { email: 'gestionnaire@recette.ma', password: 'MauvaisMotDePasse' + i } })).status);
  const r = await api('POST', '/api/auth/login', { body: { email: 'gestionnaire@recette.ma', password: 'MauvaisMotDePasse' } });
  return { ok: codes.every(c => c === 401) && r.status === 429, obtained: `${codes.join(', ')} puis ${r.status}` };
});

const passed = results.filter(r => r.ok).length;
// Temps de réponse : requêtes JSON (hors génération PDF et hors 1re requête, qui inclut le démarrage à froid)
const json = timings.slice(1).filter(t => !t.pdf).map(t => t.ms).sort((a, b) => a - b);
const pct = p => json[Math.min(json.length - 1, Math.floor(p * json.length))];
const perf = {
  requetes: timings.length,
  json: { n: json.length, moyenneMs: +(json.reduce((s, x) => s + x, 0) / json.length).toFixed(1), medianeMs: +pct(0.5).toFixed(1), p95Ms: +pct(0.95).toFixed(1), maxMs: +json[json.length - 1].toFixed(1) },
  pdfMaxMs: +Math.max(...timings.filter(t => t.pdf).map(t => t.ms)).toFixed(1),
};
console.log('Temps de réponse JSON :', perf.json, '— PDF max', perf.pdfMaxMs, 'ms');
const summary = { date: new Date().toISOString(), base: BASE, total: results.length, passed, failed: results.length - passed, perf };
fs.writeFileSync(OUT, JSON.stringify({ summary, results }, null, 2));
console.log(`\n${passed}/${results.length} cas réussis`);
process.exit(passed === results.length ? 0 : 1);
