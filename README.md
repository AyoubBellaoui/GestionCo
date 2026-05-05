# GestionCo — Application de Gestion Commerciale

Application ERP complète pour le marché marocain : **backend .NET 9** + **frontend Angular 18** + **SQL Server**.

---

## Stack technique

| Couche | Technologie |
|--------|------------|
| Backend | .NET 9, ASP.NET Core Web API, Entity Framework Core 9 |
| Frontend | Angular 18 (standalone components, lazy loading) |
| Base de données | SQL Server (LocalDB / Express / Standard) |
| Auth | JWT Bearer tokens |
| CSS | Design system custom (DM Sans + JetBrains Mono) |
| PDF | Génération HTML côté serveur, impression via `window.print()` |

---

## Prérequis

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org/)
- [Angular CLI 18](https://angular.dev/tools/cli) : `npm install -g @angular/cli`
- **SQL Server** sur `localhost` (LocalDB, Express, Developer ou Standard)

---

## Démarrage rapide

### Terminal 1 — Backend

```bash
cd backend/GestionCo.Api
dotnet run
```

Au démarrage, le backend :
- Crée automatiquement la base de données **`GestionCoDb`**
- Insère les données de démonstration
- Expose l'API REST sur **http://localhost:5000**
- Expose Swagger UI sur **http://localhost:5000/swagger**

### Terminal 2 — Frontend Angular

```bash
cd frontend
npm install        # première fois seulement 
npm start          # équivalent à : ng serve 
```

Lance l'application sur **http://localhost:4200**

---

## Configuration de la base de données

Fichier : `backend/GestionCo.Api/appsettings.json`

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=StockVenteDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

| Environnement | Connection String |
|---------------|------------------|
| SQL Server local (Windows Auth) | `Server=localhost;Database=StockVenteDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server Express | `Server=.\SQLEXPRESS;Database=StockVenteDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server LocalDB | `Server=(localdb)\MSSQLLocalDB;Database=StockVenteDb;Trusted_Connection=True;TrustServerCertificate=True;` |
| Authentification SQL (login/mdp) | `Server=localhost;Database=StockVenteDb;User Id=sa;Password=VotreMotDePasse;TrustServerCertificate=True;` |
| Docker | `Server=localhost,1433;Database=StockVenteDb;User Id=sa;Password=YourStrong@Pwd;TrustServerCertificate=True;` |

---

## Comptes de démonstration

| Rôle | Email | Mot de passe |
|------|-------|--------------|
| Admin | admin@gestionco.ma | Admin123! |
| Admin |  admin10@gestionco.ma | Admin1234  |


---

## Fonctionnalités

- **Authentification** — JWT, rôles Admin / Gestionnaire, sessions
- **Dashboard** — KPIs temps réel, graphiques, alertes stock
- **Ventes** — création, lignes produits, calcul TVA automatique
- **Achats** — fournisseurs, réception stock automatique
- **Produits** — stock, alertes seuil bas, catégories, TVA
- **Clients** — Entreprise (ICE 15 chiffres) & Particuliers
- **Fournisseurs** — historique achats, stats
- **Factures** — génération automatique, PDF imprimable, suivi paiement
- **Paiements** — espèces, carte bancaire, virement, chèque
- **Mouvements de stock** — historique complet entrées/sorties
- **Journal d'audit** — traçabilité de toutes les actions utilisateurs
- **Paramètres** — infos entreprise, profil, apparence, notifications, sécurité, facturation, comptes utilisateurs

---

## Structure du projet

```
GestionCo/
├── backend/
│   └── GestionCo.Api/              .NET 9 — Clean Architecture
│       ├── Domain/                  Entités, enums, interfaces
│       ├── Application/             CQRS — MediatR + FluentValidation
│       ├── Infrastructure/          EF Core, services, audit
│       └── Api/                     Controllers REST, middleware JWT
│
├── frontend-angular/               Angular 18 — frontend principal
│   └── src/
│       ├── app/
│       │   ├── core/
│       │   │   ├── models/          Interfaces TypeScript (DTOs)
│       │   │   ├── services/        ApiService, AuthService, ToastService, SettingsService
│       │   │   ├── guards/          AuthGuard (CanActivateFn)
│       │   │   └── interceptors/    JWT Bearer interceptor
│       │   ├── pages/               18 pages standalone
│       │   │   ├── login/
│       │   │   ├── dashboard/
│       │   │   ├── ventes/
│       │   │   ├── vente-form/
│       │   │   ├── achats/
│       │   │   ├── achat-form/
│       │   │   ├── produits/
│       │   │   ├── produit-form/
│       │   │   ├── clients/
│       │   │   ├── client-form/
│       │   │   ├── fournisseurs/
│       │   │   ├── fournisseur-form/
│       │   │   ├── categories/
│       │   │   ├── paiements/
│       │   │   ├── mouvements-stock/
│       │   │   ├── factures/
│       │   │   ├── journal-audit/
│       │   │   └── settings/
│       │   └── shared/              Composants réutilisables
│       │       ├── sidebar/
│       │       ├── topbar/
│       │       └── modal/
│       └── styles.css               Design system global (CSS variables, thème clair/sombre)
│
└── frontend/                       React 19 + Vite — version originale (optionnel)
    └── src/
        ├── pages/
        ├── components/
        ├── services/
        └── styles/
```

---

## Dépannage

### SQL Server ne démarre pas

```powershell
# Vérifier l'état des services SQL
Get-Service | Where-Object {$_.Name -like "*SQL*"}

# Démarrer le service si arrêté
Start-Service MSSQLSERVER   # ou MSSQL$SQLEXPRESS selon votre édition
```

### Activer TCP/IP (si connexion refusée)

1. Ouvrir **SQL Server Configuration Manager**
2. SQL Server Network Configuration → Protocols for MSSQLSERVER
3. Activer **TCP/IP**
4. Redémarrer le service SQL Server

### Tester la connexion

```powershell
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

### Réinitialiser la base de données

```sql
DROP DATABASE StockVenteDb;
```

Puis relancer `dotnet run` — la base est recréée avec les données de démo.

---

## Frontend React (original)

L'application existe aussi en version React 19 + Vite (interface identique) :

```bash
cd frontend
npm install
npm run dev
```

Lance sur **http://localhost:5173**
