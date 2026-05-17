using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCo.Api.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.ToTable("clients");
        b.HasKey(x => x.Id);
        b.Property(x => x.NomClient).HasMaxLength(200).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.ICE).HasMaxLength(15);
        b.Property(x => x.RC).HasMaxLength(50);
        b.Property(x => x.IF).HasMaxLength(50);
        b.Property(x => x.Adresse).HasMaxLength(500);
        b.Property(x => x.Ville).HasMaxLength(100);
        b.Property(x => x.Telephone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.PersonneContact).HasMaxLength(200);
        b.HasIndex(x => x.ICE);
    }
}

public class CategorieConfiguration : IEntityTypeConfiguration<Categorie>
{
    public void Configure(EntityTypeBuilder<Categorie> b)
    {
        b.ToTable("categories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nom).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Icone).HasMaxLength(10);
        b.HasIndex(x => x.Nom).IsUnique();
    }
}

public class FournisseurConfiguration : IEntityTypeConfiguration<Fournisseur>
{
    public void Configure(EntityTypeBuilder<Fournisseur> b)
    {
        b.ToTable("fournisseurs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nom).HasMaxLength(200).IsRequired();
        b.Property(x => x.Icone).HasMaxLength(10);
        b.Property(x => x.Telephone).HasMaxLength(30);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Adresse).HasMaxLength(500);
        b.Property(x => x.SiteWeb).HasMaxLength(200);
        b.Property(x => x.PersonneContact).HasMaxLength(200);
    }
}

public class ProduitConfiguration : IEntityTypeConfiguration<Produit>
{
    public void Configure(EntityTypeBuilder<Produit> b)
    {
        b.ToTable("produits");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        b.Property(x => x.Nom).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Image).HasMaxLength(500);
        b.Property(x => x.CodeBarre).HasMaxLength(50);
        b.Property(x => x.PrixHT).HasColumnType("decimal(18,2)");
        b.Property(x => x.PrixTTC).HasColumnType("decimal(18,2)");
        b.Property(x => x.TVA).HasColumnType("decimal(5,2)");
        b.HasIndex(x => x.Reference).IsUnique();
        b.HasIndex(x => x.CodeBarre);

        b.HasOne(x => x.Categorie)
            .WithMany(c => c.Produits)
            .HasForeignKey(x => x.CategorieId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Fournisseur)
            .WithMany(f => f.Produits)
            .HasForeignKey(x => x.FournisseurId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AchatConfiguration : IEntityTypeConfiguration<Achat>
{
    public void Configure(EntityTypeBuilder<Achat> b)
    {
        b.ToTable("achats");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        b.Property(x => x.MontantTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => x.Reference).IsUnique();

        b.HasOne(x => x.Fournisseur)
            .WithMany(f => f.Achats)
            .HasForeignKey(x => x.FournisseurId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Utilisateur)
            .WithMany()
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LigneAchatConfiguration : IEntityTypeConfiguration<LigneAchat>
{
    public void Configure(EntityTypeBuilder<LigneAchat> b)
    {
        b.ToTable("lignes_achat");
        b.HasKey(x => x.Id);
        b.Property(x => x.PrixUnitaire).HasColumnType("decimal(18,2)");
        b.Property(x => x.Remise).HasColumnType("decimal(5,2)");
        b.Ignore(x => x.Total);

        b.HasOne(x => x.Achat)
            .WithMany(a => a.Lignes)
            .HasForeignKey(x => x.AchatId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Produit)
            .WithMany(p => p.LignesAchat)
            .HasForeignKey(x => x.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class VenteConfiguration : IEntityTypeConfiguration<Vente>
{
    public void Configure(EntityTypeBuilder<Vente> b)
    {
        b.ToTable("ventes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        b.Property(x => x.MontantTotalHT).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantTVA).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantPaye).HasColumnType("decimal(18,2)");
        b.Property(x => x.Statut).HasConversion<int>();
        b.HasIndex(x => x.Reference).IsUnique();

        b.HasOne(x => x.Client)
            .WithMany(c => c.Ventes)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Utilisateur)
            .WithMany(u => u.Ventes)
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LigneVenteConfiguration : IEntityTypeConfiguration<LigneVente>
{
    public void Configure(EntityTypeBuilder<LigneVente> b)
    {
        b.ToTable("lignes_vente");
        b.HasKey(x => x.Id);
        b.Property(x => x.PrixUnitaire).HasColumnType("decimal(18,2)");
        b.Property(x => x.Remise).HasColumnType("decimal(5,2)");
        b.Property(x => x.TVA).HasColumnType("decimal(5,2)");
        b.Ignore(x => x.Total);

        b.HasOne(x => x.Vente)
            .WithMany(v => v.Lignes)
            .HasForeignKey(x => x.VenteId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Produit)
            .WithMany(p => p.LignesVente)
            .HasForeignKey(x => x.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaiementConfiguration : IEntityTypeConfiguration<Paiement>
{
    public void Configure(EntityTypeBuilder<Paiement> b)
    {
        b.ToTable("paiements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Montant).HasColumnType("decimal(18,2)");
        b.Property(x => x.Methode).HasConversion<int>();
        b.Property(x => x.Statut).HasConversion<int>();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.Vente)
            .WithMany(v => v.Paiements)
            .HasForeignKey(x => x.VenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FactureConfiguration : IEntityTypeConfiguration<Facture>
{
    public void Configure(EntityTypeBuilder<Facture> b)
    {
        b.ToTable("factures");
        b.HasKey(x => x.Id);
        b.Property(x => x.NumeroFacture).HasMaxLength(30).IsRequired();
        b.Property(x => x.Statut).HasConversion<int>();
        b.HasIndex(x => x.NumeroFacture).IsUnique();

        b.HasOne(x => x.Vente)
            .WithOne(v => v.Facture)
            .HasForeignKey<Facture>(x => x.VenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MouvementStockConfiguration : IEntityTypeConfiguration<MouvementStock>
{
    public void Configure(EntityTypeBuilder<MouvementStock> b)
    {
        b.ToTable("mouvements_stock");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Source).HasConversion<int>();
        b.Property(x => x.ReferenceText).HasMaxLength(30);
        b.Property(x => x.Raison).HasMaxLength(200);
        b.Property(x => x.Commentaire).HasMaxLength(1000);

        b.HasOne(x => x.Produit)
            .WithMany(p => p.MouvementsStock)
            .HasForeignKey(x => x.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Utilisateur)
            .WithMany()
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LogConfiguration : IEntityTypeConfiguration<Log>
{
    public void Configure(EntityTypeBuilder<Log> b)
    {
        b.ToTable("logs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasConversion<int>();
        b.Property(x => x.Entite).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntiteReference).HasMaxLength(30);
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.AnciennesValeurs).HasColumnType("nvarchar(max)");
        b.Property(x => x.NouvellesValeurs).HasColumnType("nvarchar(max)");
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);

        b.HasOne(x => x.Utilisateur)
            .WithMany(u => u.Logs)
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.DateAction);
        b.HasIndex(x => x.UtilisateurId);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Titre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Message).HasMaxLength(500).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Categorie).HasConversion<int>();
        b.Property(x => x.EntiteReference).HasMaxLength(30);
        b.Property(x => x.LienUrl).HasMaxLength(200);
        b.HasIndex(x => x.IsRead);
        b.HasIndex(x => x.CreatedAt);
    }
}

public class CategorieChargeConfiguration : IEntityTypeConfiguration<CategorieCharge>
{
    public void Configure(EntityTypeBuilder<CategorieCharge> b)
    {
        b.ToTable("categories_charge");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nom).HasMaxLength(100).IsRequired();
        b.Property(x => x.Icone).HasMaxLength(10);
        b.HasIndex(x => x.Nom).IsUnique();
    }
}

public class ChargeConfiguration : IEntityTypeConfiguration<Charge>
{
    public void Configure(EntityTypeBuilder<Charge> b)
    {
        b.ToTable("charges");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        b.Property(x => x.Titre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Montant).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantPaye).HasColumnType("decimal(18,2)");
        b.Property(x => x.Justificatif).HasMaxLength(500);
        b.Property(x => x.Statut).HasConversion<int>();
        b.HasIndex(x => x.Reference).IsUnique();

        b.HasOne(x => x.CategorieCharge)
            .WithMany(c => c.Charges)
            .HasForeignKey(x => x.CategorieChargeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Utilisateur)
            .WithMany()
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Fournisseur)
            .WithMany()
            .HasForeignKey(x => x.FournisseurId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class PaiementChargeConfiguration : IEntityTypeConfiguration<PaiementCharge>
{
    public void Configure(EntityTypeBuilder<PaiementCharge> b)
    {
        b.ToTable("paiements_charge");
        b.HasKey(x => x.Id);
        b.Property(x => x.Montant).HasColumnType("decimal(18,2)");
        b.Property(x => x.Methode).HasConversion<int>();
        b.Property(x => x.Statut).HasConversion<int>();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.Charge)
            .WithMany(c => c.Paiements)
            .HasForeignKey(x => x.ChargeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DevisConfiguration : IEntityTypeConfiguration<Devis>
{
    public void Configure(EntityTypeBuilder<Devis> b)
    {
        b.ToTable("devis");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reference).HasMaxLength(30).IsRequired();
        b.Property(x => x.MontantTotalHT).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantTVA).HasColumnType("decimal(18,2)");
        b.Property(x => x.MontantTotal).HasColumnType("decimal(18,2)");
        b.Property(x => x.Statut).HasConversion<int>();
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.HasIndex(x => x.Reference).IsUnique();

        b.HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Utilisateur)
            .WithMany()
            .HasForeignKey(x => x.UtilisateurId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Vente)
            .WithMany()
            .HasForeignKey(x => x.VenteId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ParametresFacturationConfiguration : IEntityTypeConfiguration<ParametresFacturation>
{
    public void Configure(EntityTypeBuilder<ParametresFacturation> b)
    {
        b.ToTable("parametres_facturation");
        b.HasKey(x => x.Id);
        b.Property(x => x.PrefixeFacture).HasMaxLength(10).IsRequired();
        b.Property(x => x.PrefixeVente).HasMaxLength(10).IsRequired();
        b.Property(x => x.PrefixeAchat).HasMaxLength(10).IsRequired();
        b.Property(x => x.PrefixeProduit).HasMaxLength(10).IsRequired();
    }
}

public class ParametresEntrepriseConfiguration : IEntityTypeConfiguration<ParametresEntreprise>
{
    public void Configure(EntityTypeBuilder<ParametresEntreprise> b)
    {
        b.ToTable("parametres_entreprise");
        b.HasKey(x => x.Id);
        b.Property(x => x.RaisonSociale).HasMaxLength(200).IsRequired();
        b.Property(x => x.Adresse).HasMaxLength(500);
        b.Property(x => x.Telephone).HasMaxLength(50);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Ice).HasMaxLength(50);
        b.Property(x => x.Rc).HasMaxLength(100);
        b.Property(x => x.If).HasMaxLength(100);
        b.Property(x => x.Patente).HasMaxLength(100);
        b.Property(x => x.Cnss).HasMaxLength(100);
        b.Property(x => x.Capital).HasMaxLength(100);
        b.Property(x => x.Rib).HasMaxLength(100);
        b.Property(x => x.Banque).HasMaxLength(200);
        b.Property(x => x.Swift).HasMaxLength(20);
        b.Property(x => x.Logo).HasMaxLength(2000);
    }
}

public class LigneDevisConfiguration : IEntityTypeConfiguration<LigneDevis>
{
    public void Configure(EntityTypeBuilder<LigneDevis> b)
    {
        b.ToTable("lignes_devis");
        b.HasKey(x => x.Id);
        b.Property(x => x.PrixUnitaire).HasColumnType("decimal(18,2)");
        b.Property(x => x.Remise).HasColumnType("decimal(5,2)");
        b.Property(x => x.Tva).HasColumnType("decimal(5,2)");
        b.Ignore(x => x.Total);

        b.HasOne(x => x.Devis)
            .WithMany(d => d.Lignes)
            .HasForeignKey(x => x.DevisId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Produit)
            .WithMany()
            .HasForeignKey(x => x.ProduitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
