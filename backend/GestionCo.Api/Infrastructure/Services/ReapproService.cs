using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Infrastructure.Services;

public class ReapproService : IReapproService
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly INotificationService _notif;

    public ReapproService(IAppDbContext db, IReferenceGenerator refGen, INotificationService notif)
    {
        _db = db; _refGen = refGen; _notif = notif;
    }

    public async Task<ReapproResultDto> TryGenererReapproAsync(int produitId, int utilisateurId, CancellationToken ct = default)
    {
        var produit = await _db.Produits
            .Include(p => p.Fournisseur)
            .FirstOrDefaultAsync(p => p.Id == produitId, ct);

        if (produit == null || produit.QuantiteReappro <= 0 || !produit.FournisseurId.HasValue)
            return new ReapproResultDto { Created = false, Message = "Produit non éligible (fournisseur ou quantité de réappro non définis)" };

        // Avoid duplicate: any EnAttente achat with this product in last 7 days
        var since = DateTime.UtcNow.AddDays(-7);
        var alreadyPending = await _db.Achats
            .AnyAsync(a => a.Statut == StatutAchat.EnAttente &&
                           a.CreatedAt >= since &&
                           a.Lignes.Any(l => l.ProduitId == produitId), ct);

        if (alreadyPending)
            return new ReapproResultDto { Created = false, Message = "Un bon de commande en attente existe déjà pour ce produit (< 7 jours)" };

        var reference = await _refGen.GeneratePurchaseReferenceAsync(ct);

        var achat = new Achat
        {
            Reference = reference,
            FournisseurId = produit.FournisseurId.Value,
            UtilisateurId = utilisateurId,
            DateAchat = DateTime.UtcNow,
            Statut = StatutAchat.EnAttente,
            Notes = $"Réapprovisionnement automatique — {produit.Nom} (stock actuel : {produit.QuantiteStock}, seuil : {produit.SeuilAlerte})",
        };

        achat.Lignes.Add(new LigneAchat
        {
            ProduitId = produit.Id,
            Quantite = produit.QuantiteReappro,
            PrixUnitaire = produit.PrixHT,
        });
        achat.MontantTotal = produit.QuantiteReappro * produit.PrixHT;

        _db.Achats.Add(achat);
        await _db.SaveChangesAsync(ct);

        await _notif.CreateAsync(
            titre: $"Réappro auto — {produit.Nom}",
            message: $"Bon de commande {reference} généré ({produit.QuantiteReappro} unité(s) chez {produit.Fournisseur!.Nom}). Stock actuel : {produit.QuantiteStock}/{produit.SeuilAlerte}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Stock,
            entiteId: achat.Id,
            entiteReference: reference,
            lienUrl: "/achats",
            ct: ct);

        return new ReapproResultDto { Created = true, AchatId = achat.Id, Reference = reference };
    }
}
