using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Infrastructure.Persistence.Seeds;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, CancellationToken ct = default)
    {
        // La DB est déjà créée par Program.cs via EnsureCreatedAsync
        if (await db.Produits.AnyAsync(ct)) return; // Déjà seedé

        // ============ UTILISATEURS ============
        var admin = new Utilisateur
        {
            Nom = "Benali", Prenom = "Youssef",
            Email = "admin@gestionco.ma",
            PasswordHash = hasher.Hash("Admin123!"),
            Telephone = "+212 6 61 23 45 67",
            Role = RoleUtilisateur.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-18)
        };

        var admin2 = new Utilisateur
        {
            Nom = "Berrada", Prenom = "Mohamed",
            Email = "m.berrada@gestionco.ma",
            PasswordHash = hasher.Hash("Admin123!"),
            Telephone = "+212 6 62 34 56 78",
            Role = RoleUtilisateur.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-12)
        };

        var manager = new Utilisateur
        {
            Nom = "Fassi", Prenom = "Salma",
            Email = "s.fassi@gestionco.ma",
            PasswordHash = hasher.Hash("Manager123!"),
            Telephone = "+212 6 63 45 67 89",
            Role = RoleUtilisateur.Gestionnaire,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMonths(-6)
        };

        db.Utilisateurs.AddRange(admin, admin2, manager);
        await db.SaveChangesAsync(ct);

        // ============ CATÉGORIES CHARGES ============
        var chargeCategories = new List<CategorieCharge>
        {
            new() { Nom = "Loyer", Icone = "🏢" },
            new() { Nom = "Électricité", Icone = "⚡" },
            new() { Nom = "Internet", Icone = "🌐" },
            new() { Nom = "Transport", Icone = "🚗" },
            new() { Nom = "Salaires", Icone = "👤" },
            new() { Nom = "Maintenance", Icone = "🔧" },
            new() { Nom = "Eau", Icone = "💧" },
            new() { Nom = "Assurance", Icone = "🛡️" },
            new() { Nom = "Fournitures bureau", Icone = "📎" },
            new() { Nom = "Marketing", Icone = "📣" },
            new() { Nom = "Autres", Icone = "📋" }
        };
        db.CategoriesCharge.AddRange(chargeCategories);
        await db.SaveChangesAsync(ct);

        // ============ CATÉGORIES ============
        var catInfo = new Categorie { Nom = "Informatique", Icone = "💻", Description = "Ordinateurs, périphériques" };
        var catMobilier = new Categorie { Nom = "Mobilier bureau", Icone = "🪑", Description = "Chaises, bureaux, rangement" };
        var catFournitures = new Categorie { Nom = "Fournitures", Icone = "📦", Description = "Papeterie, consommables" };
        var catElectro = new Categorie { Nom = "Électronique", Icone = "⚡", Description = "Composants, accessoires" };

        db.Categories.AddRange(catInfo, catMobilier, catFournitures, catElectro);
        await db.SaveChangesAsync(ct);

        // ============ FOURNISSEURS ============
        var fournTech = new Fournisseur
        {
            Nom = "TechImport Maroc SARL",
            Icone = "💻",
            Telephone = "+212 5 22 30 40 50",
            Email = "contact@techimport.ma",
            Adresse = "25 Zone Industrielle Sidi Maarouf, Casablanca",
            SiteWeb = "https://www.techimport.ma",
            PersonneContact = "M. Karim Atlas",
            IsActive = true
        };
        var fournMob = new Fournisseur
        {
            Nom = "Mobilier Pro Distribution",
            Icone = "🪑",
            Telephone = "+212 5 37 70 80 90",
            Email = "commercial@mobilierpro.ma",
            Adresse = "Rte de Rabat, Salé",
            IsActive = true
        };
        var fournElec = new Fournisseur
        {
            Nom = "ElectroWorld Maroc",
            Icone = "⚡",
            Telephone = "+212 5 22 45 67 89",
            Email = "info@electroworld.ma",
            Adresse = "Quartier industriel Ain Sebaa, Casablanca",
            IsActive = true
        };

        db.Fournisseurs.AddRange(fournTech, fournMob, fournElec);
        await db.SaveChangesAsync(ct);

        // ============ PRODUITS ============
        var year = DateTime.UtcNow.Year;
        var produits = new List<Produit>
        {
            new() { Reference = $"PRD-{year}-0001", Nom = "Ordinateur portable ProMax 15\"",
                    Description = "Laptop 15.6\", i7, 16GB RAM, 512GB SSD",
                    Image = "💻", PrixHT = 8500, TVA = 20, PrixTTC = 10200,
                    QuantiteStock = 24, SeuilAlerte = 5,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id,
                    CodeBarre = "6110123456001" },
            new() { Reference = $"PRD-{year}-0002", Nom = "Écran 27\" 4K IPS",
                    Description = "Moniteur 27 pouces UHD 4K",
                    Image = "🖥", PrixHT = 2800, TVA = 20, PrixTTC = 3360,
                    QuantiteStock = 4, SeuilAlerte = 5,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id,
                    CodeBarre = "6110123456002" },
            new() { Reference = $"PRD-{year}-0003", Nom = "Disque SSD 1TB NVMe",
                    Description = "SSD M.2 NVMe 1TB haute performance",
                    Image = "💾", PrixHT = 950, TVA = 20, PrixTTC = 1140,
                    QuantiteStock = 0, SeuilAlerte = 10,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id,
                    CodeBarre = "6110123456003" },
            new() { Reference = $"PRD-{year}-0004", Nom = "Clavier mécanique RGB",
                    Description = "Clavier gaming rétroéclairé RGB",
                    Image = "🎹", PrixHT = 625, TVA = 20, PrixTTC = 750,
                    QuantiteStock = 18, SeuilAlerte = 5,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id },
            new() { Reference = $"PRD-{year}-0005", Nom = "Souris sans fil ergonomique",
                    Description = "Souris Bluetooth ergonomique",
                    Image = "🖱", PrixHT = 180, TVA = 20, PrixTTC = 216,
                    QuantiteStock = 42, SeuilAlerte = 10,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id },
            new() { Reference = $"PRD-{year}-0006", Nom = "Chaise ergonomique Comfort+",
                    Description = "Chaise de bureau ergonomique avec support lombaire",
                    Image = "🪑", PrixHT = 1500, TVA = 20, PrixTTC = 1800,
                    QuantiteStock = 12, SeuilAlerte = 3,
                    CategorieId = catMobilier.Id, FournisseurId = fournMob.Id },
            new() { Reference = $"PRD-{year}-0007", Nom = "Bureau 160x80 cm",
                    Description = "Bureau moderne en bois MDF",
                    Image = "🪑", PrixHT = 2200, TVA = 20, PrixTTC = 2640,
                    QuantiteStock = 8, SeuilAlerte = 3,
                    CategorieId = catMobilier.Id, FournisseurId = fournMob.Id },
            new() { Reference = $"PRD-{year}-0008", Nom = "Imprimante multifonction",
                    Description = "Imprimante couleur laser WiFi",
                    Image = "🖨", PrixHT = 1250, TVA = 20, PrixTTC = 1500,
                    QuantiteStock = 6, SeuilAlerte = 3,
                    CategorieId = catInfo.Id, FournisseurId = fournTech.Id },
            new() { Reference = $"PRD-{year}-0009", Nom = "Webcam HD 1080p",
                    Description = "Webcam HD avec micro intégré",
                    Image = "📷", PrixHT = 450, TVA = 20, PrixTTC = 540,
                    QuantiteStock = 15, SeuilAlerte = 5,
                    CategorieId = catElectro.Id, FournisseurId = fournElec.Id },
            new() { Reference = $"PRD-{year}-0010", Nom = "Casque audio bluetooth",
                    Description = "Casque sans fil avec réduction de bruit",
                    Image = "🎧", PrixHT = 850, TVA = 20, PrixTTC = 1020,
                    QuantiteStock = 22, SeuilAlerte = 5,
                    CategorieId = catElectro.Id, FournisseurId = fournElec.Id }
        };

        db.Produits.AddRange(produits);
        await db.SaveChangesAsync(ct);

        // ============ CLIENTS ============
        // D'abord créer les users client
        var userAtlas = new Utilisateur
        {
            Nom = "Atlas", Prenom = "Karim",
            Email = "direction@atlas.ma",
            PasswordHash = hasher.Hash("Client123!"),
            Telephone = "+212 5 22 34 56 78",
            Role = RoleUtilisateur.Client,
            IsActive = true
        };
        db.Utilisateurs.Add(userAtlas);
        await db.SaveChangesAsync(ct);

        var clients = new List<Client>
        {
            new() { NomClient = "Société Atlas SARL", Type = TypeClient.Entreprise,
                    ICE = "001234567890012", RC = "125678", IF = "12345678",
                    Adresse = "25 Rue Hassan II, Quartier des Hôpitaux", Ville = "Casablanca",
                    Telephone = "+212 5 22 34 56 78", Email = "direction@atlas.ma",
                    PersonneContact = "M. Karim Atlas", IsActive = true,
                    UtilisateurId = userAtlas.Id },
            new() { NomClient = "Maroc Distribution", Type = TypeClient.Entreprise,
                    ICE = "005678901234056", Adresse = "Zone industrielle Moulay Rachid",
                    Ville = "Casablanca", Telephone = "+212 5 22 56 78 90",
                    Email = "contact@marocdist.ma", IsActive = true },
            new() { NomClient = "TechnoPlus Maroc", Type = TypeClient.Entreprise,
                    ICE = "009876543210098", Adresse = "Agdal, Rabat", Ville = "Rabat",
                    Telephone = "+212 5 37 12 34 56", Email = "contact@technoplus.ma",
                    IsActive = true },
            new() { NomClient = "BTP Maroc SARL", Type = TypeClient.Entreprise,
                    ICE = "003456789012034", Adresse = "Avenue Mohamed V", Ville = "Tanger",
                    Telephone = "+212 5 39 12 34 56", Email = "info@btpmaroc.ma",
                    IsActive = true },
            new() { NomClient = "Karim El Amrani", Type = TypeClient.Particulier,
                    Telephone = "+212 6 61 23 45 67", Email = "k.elamrani@gmail.com",
                    Ville = "Casablanca", IsActive = true },
            new() { NomClient = "Fatima Zahra Bennis", Type = TypeClient.Particulier,
                    Telephone = "+212 6 62 34 56 78", Email = "f.bennis@outlook.com",
                    Ville = "Rabat", IsActive = true },
            new() { NomClient = "Hassan Tazi", Type = TypeClient.Particulier,
                    Telephone = "+212 6 63 45 67 89", Email = "h.tazi@outlook.com",
                    Ville = "Casablanca", IsActive = true }
        };

        db.Clients.AddRange(clients);
        await db.SaveChangesAsync(ct);

        // ============ VENTES + FACTURES + PAIEMENTS ============
        await SeedVentesAsync(db, produits, clients, admin, ct);
    }

    private static async Task SeedVentesAsync(
        AppDbContext db,
        List<Produit> produits,
        List<Client> clients,
        Utilisateur admin,
        CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var now = DateTime.UtcNow;

        // Vente 1 - payée récente - Société Atlas
        var vente1 = new Vente
        {
            Reference = $"VNT-{year}-0001",
            ClientId = clients[0].Id,
            UtilisateurId = admin.Id,
            DateVente = now.AddDays(-1),
            DateEcheance = now.AddDays(29),
            Statut = StatutVente.Paye,
            Lignes = new List<LigneVente>
            {
                new() { ProduitId = produits[0].Id, Quantite = 2, PrixUnitaire = 8500, TVA = 20 },
                new() { ProduitId = produits[1].Id, Quantite = 2, PrixUnitaire = 2800, TVA = 20 }
            }
        };
        vente1.MontantTotalHT = vente1.Lignes.Sum(l => l.Total);
        vente1.MontantTVA = vente1.MontantTotalHT * 0.20m;
        vente1.MontantTotal = vente1.MontantTotalHT + vente1.MontantTVA;
        vente1.MontantPaye = vente1.MontantTotal;

        db.Ventes.Add(vente1);
        await db.SaveChangesAsync(ct);

        db.Paiements.Add(new Paiement
        {
            VenteId = vente1.Id,
            Montant = vente1.MontantTotal,
            Methode = MethodePaiement.Virement,
            Statut = StatutPaiement.Confirme,
            DatePaiement = now.AddDays(-1),
            Reference = "VIR-20260417-4532"
        });

        db.Factures.Add(new Facture
        {
            NumeroFacture = $"FAC-{year}-001",
            VenteId = vente1.Id,
            DateEmission = now.AddDays(-1),
            DateEcheance = now.AddDays(29),
            Statut = StatutFacture.Payee,
            EstEnvoyeeEmail = true,
            DateEnvoiEmail = now.AddDays(-1)
        });

        // Vente 2 - en attente - TechnoPlus
        var vente2 = new Vente
        {
            Reference = $"VNT-{year}-0002",
            ClientId = clients[2].Id,
            UtilisateurId = admin.Id,
            DateVente = now.AddDays(-5),
            DateEcheance = now.AddDays(25),
            Statut = StatutVente.EnAttente,
            Lignes = new List<LigneVente>
            {
                new() { ProduitId = produits[4].Id, Quantite = 10, PrixUnitaire = 180, TVA = 20 },
                new() { ProduitId = produits[8].Id, Quantite = 5, PrixUnitaire = 450, TVA = 20 }
            }
        };
        vente2.MontantTotalHT = vente2.Lignes.Sum(l => l.Total);
        vente2.MontantTVA = vente2.MontantTotalHT * 0.20m;
        vente2.MontantTotal = vente2.MontantTotalHT + vente2.MontantTVA;

        db.Ventes.Add(vente2);
        await db.SaveChangesAsync(ct);

        db.Factures.Add(new Facture
        {
            NumeroFacture = $"FAC-{year}-002",
            VenteId = vente2.Id,
            DateEmission = now.AddDays(-5),
            DateEcheance = now.AddDays(25),
            Statut = StatutFacture.EnAttente
        });

        await db.SaveChangesAsync(ct);
    }
}
