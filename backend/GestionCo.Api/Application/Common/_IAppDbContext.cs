using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Utilisateur> Utilisateurs { get; }
    DbSet<Client> Clients { get; }
    DbSet<Categorie> Categories { get; }
    DbSet<Fournisseur> Fournisseurs { get; }
    DbSet<Produit> Produits { get; }
    DbSet<Achat> Achats { get; }
    DbSet<LigneAchat> LignesAchat { get; }
    DbSet<Vente> Ventes { get; }
    DbSet<LigneVente> LignesVente { get; }
    DbSet<Paiement> Paiements { get; }
    DbSet<Facture> Factures { get; }
    DbSet<MouvementStock> MouvementsStock { get; }
    DbSet<Log> Logs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
