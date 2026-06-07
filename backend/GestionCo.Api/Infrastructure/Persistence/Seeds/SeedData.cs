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
        if (await db.Categories.AnyAsync(ct)) return; // Déjà seedé

        // ============ UTILISATEURS ============
        var admin = new Utilisateur
        {
            Nom = "Bellaoui", Prenom = "Ayoub",
            Email = "admin@gestionco.ma",
            PasswordHash = hasher.Hash("Admin123!"),
            Role = RoleUtilisateur.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Utilisateurs.Add(admin);
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
        db.Categories.AddRange(
            new Categorie { Nom = "Informatique", Icone = "💻", Description = "Ordinateurs, périphériques" },
            new Categorie { Nom = "Mobilier bureau", Icone = "🪑", Description = "Chaises, bureaux, rangement" },
            new Categorie { Nom = "Fournitures", Icone = "📦", Description = "Papeterie, consommables" },
            new Categorie { Nom = "Électronique", Icone = "⚡", Description = "Composants, accessoires" }
        );
        await db.SaveChangesAsync(ct);
    }
}
