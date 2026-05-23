using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using DevisEntity = GestionCo.Api.Domain.Entities.Devis;
using LigneDevisEntity = GestionCo.Api.Domain.Entities.LigneDevis;

namespace GestionCo.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Fournisseur> Fournisseurs => Set<Fournisseur>();
    public DbSet<Produit> Produits => Set<Produit>();
    public DbSet<Achat> Achats => Set<Achat>();
    public DbSet<LigneAchat> LignesAchat => Set<LigneAchat>();
    public DbSet<PaiementAchat> PaiementsAchat => Set<PaiementAchat>();
    public DbSet<Vente> Ventes => Set<Vente>();
    public DbSet<LigneVente> LignesVente => Set<LigneVente>();
    public DbSet<Paiement> Paiements => Set<Paiement>();
    public DbSet<Facture> Factures => Set<Facture>();
    public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<CategorieCharge> CategoriesCharge => Set<CategorieCharge>();
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<PaiementCharge> PaiementsCharge => Set<PaiementCharge>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DevisEntity> Devis => Set<DevisEntity>();
    public DbSet<LigneDevisEntity> LignesDevis => Set<LigneDevisEntity>();
    public DbSet<Commande> Commandes => Set<Commande>();
    public DbSet<LigneCommande> LignesCommande => Set<LigneCommande>();
    public DbSet<Domain.Entities.ParametresFacturation> ParametresFacturation => Set<Domain.Entities.ParametresFacturation>();
    public DbSet<Domain.Entities.ParametresEntreprise> ParametresEntreprise => Set<Domain.Entities.ParametresEntreprise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Appliquer toutes les configurations IEntityTypeConfiguration du projet
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit automatique pour les entités AuditableEntity
        var userId = _currentUserService?.UserId;
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Domain.Common.AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = userId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
