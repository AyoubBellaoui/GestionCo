# 🔧 Backend GestionCo.Api

API REST .NET 9 — Clean Architecture avec CQRS.

## ⚡ Démarrage rapide

```bash
# 1. Restaurer les dépendances
dotnet restore

# 2. Créer et appliquer la migration initiale
dotnet ef migrations add InitialCreate
dotnet ef database update

# 3. Lancer l'API
dotnet run
```

Swagger UI : **http://localhost:5000/swagger**

## 📦 Packages NuGet utilisés

| Package | Usage |
|---------|-------|
| Microsoft.EntityFrameworkCore.SqlServer | ORM SQL Server |
| MediatR | CQRS pattern |
| FluentValidation.AspNetCore | Validations |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT |
| BCrypt.Net-Next | Hash passwords |
| Swashbuckle.AspNetCore | Swagger |
| AutoMapper | DTO mapping |
| QuestPDF | Génération PDF |
| CsvHelper | Export CSV |

## 🗄️ Base de données

### Migrations

```bash
# Créer une nouvelle migration
dotnet ef migrations add NomDeMaMigration

# Appliquer les migrations
dotnet ef database update

# Annuler la dernière migration
dotnet ef migrations remove

# Script SQL
dotnet ef migrations script
```

### Schéma

13 tables :
- `utilisateurs` (admin / gestionnaire / client)
- `clients` (B2B entreprise / particulier)
- `categories`
- `fournisseurs`
- `produits`
- `achats` + `lignes_achat`
- `ventes` + `lignes_vente`
- `paiements`
- `factures`
- `mouvements_stock`
- `logs` (audit)

### Seed

La première exécution crée automatiquement des données de démo :
- 3 utilisateurs (admin, admin2, manager)
- 1 utilisateur client (Atlas)
- 4 catégories
- 3 fournisseurs
- 10 produits
- 7 clients
- Quelques ventes + factures

## 🔒 Authentification

### Connection

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@gestionco.ma",
  "password": "Admin123!"
}
```

Réponse :
```json
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "base64...",
  "user": { "id": 1, "nom": "Benali", ... }
}
```

### Utiliser le token

```http
GET /api/produits
Authorization: Bearer eyJhbGc...
```

## 🎯 Rôles & Policies

- **Admin** → toutes les actions
- **Gestionnaire** → CRUD produits/ventes/achats/clients
- **Client** → accès à son espace (ses ventes/factures)

Policies déclarées :
- `AdminOnly` — Admin uniquement
- `AdminOrManager` — Admin ou Gestionnaire
- `ClientOnly` — Client uniquement

## 🏗️ Architecture Clean

### Flow CQRS

```
Controller → MediatR → Command/Query Handler → DbContext → DB
                ↓
         ValidationBehavior (FluentValidation)
```

### Exemple : créer un produit

```csharp
// 1. Controller
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProduitDto dto, CancellationToken ct)
    => Ok(await _mediator.Send(new CreateProduitCommand(dto), ct));

// 2. Validator (auto-appliqué via ValidationBehavior)
public class CreateProduitValidator : AbstractValidator<CreateProduitCommand> { ... }

// 3. Handler
public class CreateProduitHandler : IRequestHandler<CreateProduitCommand, ProduitDto>
{
    public async Task<ProduitDto> Handle(CreateProduitCommand req, CancellationToken ct)
    {
        var ref = await _refGen.GenerateProductReferenceAsync(ct);
        var produit = new Produit { ... };
        _db.Produits.Add(produit);
        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(ActionLog.Create, ...);
        return MapToDto(produit);
    }
}
```

## 📊 Logique métier automatique

### Création de vente
1. Vérifier stock disponible
2. Créer la vente + lignes
3. **Décrémenter** stock + créer mouvement (source=Vente)
4. Créer **facture automatiquement** (FAC-YYYY-XXX)
5. Si paiement initial → créer paiement + mettre à jour statut
6. Logger l'action

### Création d'achat
1. Créer l'achat + lignes
2. **Incrémenter** stock + créer mouvement (source=Achat)
3. Logger l'action

### Création de paiement
1. Vérifier vente non annulée
2. Vérifier montant ≤ reste dû
3. Créer paiement
4. Mettre à jour `vente.MontantPaye`
5. Si totalement payé → statut=Payé + facture=Payée
6. Logger l'action

### Annulation de vente
1. Vérifier pas déjà annulée
2. **Restaurer** stock + créer mouvements inverses
3. Marquer vente = Annulée
4. Marquer facture = Annulée
5. Logger l'action (sensible)

## 🧪 Tests rapides

```bash
# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@gestionco.ma","password":"Admin123!"}'

# GET produits (avec token)
TOKEN="votre_token_ici"
curl http://localhost:5000/api/produits -H "Authorization: Bearer $TOKEN"

# Dashboard stats
curl http://localhost:5000/api/dashboard/stats -H "Authorization: Bearer $TOKEN"
```

## 📄 Export facture PDF

```bash
curl http://localhost:5000/api/factures/1/pdf \
  -H "Authorization: Bearer $TOKEN" \
  --output facture.pdf
```
