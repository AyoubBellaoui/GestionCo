# GestionCo — ERP de Gestion Commerciale

Application web de gestion commerciale complète : ventes, achats, stock, clients, fournisseurs, charges, factures, rapports financiers et tableau de bord en temps réel.

**Stack :** Angular 18 (frontend) · .NET 9 / ASP.NET Core (backend) · SQL Server · JWT Auth

---

## Démarrage rapide

### Backend
```bash
cd backend/GestionCo.Api
dotnet run
# API disponible sur http://localhost:5000
```

### Frontend
```bash
cd frontend
npm install
ng serve
# Application disponible sur http://localhost:4200
```

> La base de données est créée automatiquement au premier démarrage via `EnsureCreatedAsync()` avec les données de démo pré-chargées.

---

## Comptes par défaut (créés automatiquement à l'initialisation de la BD)

> Ces comptes sont insérés automatiquement par `SeedData.cs` lors de la première création de la base de données.
> **Changez les mots de passe avant toute mise en production.**

### Compte par défaut

| Nom | Email | Mot de passe | Rôle |
|-----|-------|--------------|------|
| Ayoub Bellaoui | `admin@gestionco.ma` | `Admin123!` | Admin |

> Les comptes Gestionnaire sont créés depuis l'interface **Paramètres → Utilisateurs** par l'administrateur.

---

## Rôles et permissions

L'application distingue deux rôles principaux : **Admin** et **Gestionnaire**.

### Admin — Accès complet

L'administrateur a un contrôle total sur l'ensemble de l'application.

| Module | Permissions |
|--------|-------------|
| Tableau de bord | Lecture complète |
| Devis | Créer, modifier, supprimer (brouillons), convertir en vente |
| Ventes | Créer, consulter, **annuler** |
| Achats | Créer, consulter, enregistrer paiements |
| Produits | Créer, modifier, **supprimer**, ajuster le stock |
| Catégories | Créer, modifier, **supprimer** |
| Clients | Créer, modifier, **supprimer** |
| Fournisseurs | Créer, modifier, **supprimer** |
| Charges | Créer, modifier, enregistrer paiements |
| Paiements | Créer, consulter |
| Factures | Créer, consulter, envoyer |
| Rapports (P&L, TVA) | Accès complet |
| Mouvements stock | Consulter, **ajuster manuellement** |
| **Journal d'audit** | **Accès complet** |
| **Paramètres** | **Accès complet** (entreprise, facturation, notifications) |
| **Utilisateurs** | **Créer, modifier, désactiver, supprimer** |

---

### Gestionnaire — Accès opérationnel

Le gestionnaire gère les opérations quotidiennes mais n'a pas accès à la configuration ni à l'administration des comptes.

| Module | Permissions |
|--------|-------------|
| Tableau de bord | Lecture complète |
| Devis | Créer, modifier, supprimer (brouillons), convertir en vente |
| Ventes | Créer, consulter *(annulation impossible)* |
| Achats | Créer, consulter, enregistrer paiements |
| Produits | Créer, modifier *(suppression impossible)*, ajuster le stock |
| Catégories | Créer, modifier *(suppression impossible)* |
| Clients | Créer, modifier *(suppression impossible)* |
| Fournisseurs | Créer, modifier *(suppression impossible)* |
| Charges | Créer, modifier, enregistrer paiements |
| Paiements | Créer, consulter |
| Factures | Créer, consulter, envoyer |
| Rapports (P&L, TVA) | Accès complet |
| Mouvements stock | Consulter, ajuster manuellement |
| **Journal d'audit** | **Accès refusé** |
| **Paramètres** | **Accès refusé** |
| **Utilisateurs** | **Accès refusé** (ne peut pas créer ni gérer des comptes) |

---

### Différences résumées Admin vs Gestionnaire

| Action | Admin | Gestionnaire |
|--------|:-----:|:------------:|
| Accéder aux Paramètres | ✅ | ❌ |
| Accéder au Journal d'audit | ✅ | ❌ |
| Créer / gérer des comptes utilisateurs | ✅ | ❌ |
| Supprimer un produit | ✅ | ❌ |
| Supprimer un client | ✅ | ❌ |
| Supprimer un fournisseur | ✅ | ❌ |
| Supprimer une catégorie | ✅ | ❌ |
| Annuler une vente | ✅ | ❌ |
| Ajuster le stock manuellement | ✅ | ✅ |
| Créer des ventes / achats / devis | ✅ | ✅ |
| Enregistrer des paiements | ✅ | ✅ |
| Consulter les rapports financiers | ✅ | ✅ |
| Créer / modifier des produits | ✅ | ✅ |
| Créer / modifier des clients | ✅ | ✅ |

---

## Architecture

```
GestionCo/
├── backend/
│   └── GestionCo.Api/
│       ├── Api/Controllers/          # Endpoints REST
│       ├── Application/              # CQRS — Commands, Queries, DTOs
│       ├── Domain/                   # Entités, Enums
│       └── Infrastructure/           # EF Core, Services, Seeds
└── frontend/
    └── src/app/
        ├── core/
        │   ├── guards/               # authGuard, adminGuard
        │   ├── services/             # ApiService, AuthService, SettingsService…
        │   └── models/               # Interfaces TypeScript
        ├── pages/                    # Composants par module
        └── shared/                   # Topbar, Sidebar, Modal…
```

### Sécurité

- **JWT** — tokens d'accès (60 min) + refresh tokens (7 jours)
- **Policies backend** — `AdminOnly` et `AdminOrManager` appliquées sur chaque endpoint sensible
- **Guards frontend** — `authGuard` (toutes les pages protégées) + `adminGuard` (Paramètres, Journal d'audit)
- **UI conditionnelle** — les boutons de suppression et d'annulation sont masqués pour les Gestionnaires

---

## Configuration

Fichier : `backend/GestionCo.Api/appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=GestionCoDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "...",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  }
}
```
