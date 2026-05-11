using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IAppDbContext _db;

    public NotificationService(IAppDbContext db) => _db = db;

    public async Task CreateAsync(
        string titre,
        string message,
        TypeNotification type = TypeNotification.Info,
        CategorieNotification categorie = CategorieNotification.Systeme,
        int? entiteId = null,
        string? entiteReference = null,
        string? lienUrl = null,
        CancellationToken ct = default)
    {
        _db.Notifications.Add(new Notification
        {
            Titre = titre,
            Message = message,
            Type = type,
            Categorie = categorie,
            EntiteId = entiteId,
            EntiteReference = entiteReference,
            LienUrl = lienUrl,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task CreateStockAlertAsync(int produitId, string nomProduit, int quantiteStock, int seuilAlerte, CancellationToken ct = default)
    {
        if (quantiteStock == 0)
        {
            await CreateAsync(
                titre: $"Rupture de stock — {nomProduit}",
                message: $"Le produit « {nomProduit} » est en rupture de stock (0 unité restante).",
                type: TypeNotification.Danger,
                categorie: CategorieNotification.Stock,
                entiteId: produitId,
                lienUrl: "/produits",
                ct: ct);
        }
        else if (quantiteStock <= seuilAlerte)
        {
            await CreateAsync(
                titre: $"Stock faible — {nomProduit}",
                message: $"Le produit « {nomProduit} » est presque épuisé ({quantiteStock} unité{(quantiteStock > 1 ? "s" : "")} restante{(quantiteStock > 1 ? "s" : "")}, seuil : {seuilAlerte}).",
                type: TypeNotification.Warning,
                categorie: CategorieNotification.Stock,
                entiteId: produitId,
                lienUrl: "/produits",
                ct: ct);
        }
    }
}
