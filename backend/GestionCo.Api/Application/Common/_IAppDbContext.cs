using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DevisEntity = GestionCo.Api.Domain.Entities.Devis;
using LigneDevisEntity = GestionCo.Api.Domain.Entities.LigneDevis;

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
    DbSet<PaiementAchat> PaiementsAchat { get; }
    DbSet<Vente> Ventes { get; }
    DbSet<LigneVente> LignesVente { get; }
    DbSet<Paiement> Paiements { get; }
    DbSet<Facture> Factures { get; }
    DbSet<MouvementStock> MouvementsStock { get; }
    DbSet<Log> Logs { get; }
    DbSet<CategorieCharge> CategoriesCharge { get; }
    DbSet<Charge> Charges { get; }
    DbSet<PaiementCharge> PaiementsCharge { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<DevisEntity> Devis { get; }
    DbSet<LigneDevisEntity> LignesDevis { get; }
    DbSet<Commande> Commandes { get; }
    DbSet<LigneCommande> LignesCommande { get; }
    DbSet<ParametresFacturation> ParametresFacturation { get; }
    DbSet<ParametresEntreprise> ParametresEntreprise { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
