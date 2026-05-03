using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionCo.Api.Infrastructure.Persistence.Configurations;

public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> b)
    {
        b.ToTable("utilisateurs");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nom).HasMaxLength(100).IsRequired();
        b.Property(x => x.Prenom).HasMaxLength(100).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        b.Property(x => x.Telephone).HasMaxLength(30);
        b.Property(x => x.Role).HasConversion<int>();
        b.Property(x => x.RefreshToken).HasMaxLength(500);
        b.HasIndex(x => x.Email).IsUnique();

        b.HasOne(x => x.Client)
            .WithOne(c => c.Utilisateur)
            .HasForeignKey<Utilisateur>(x => x.ClientId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
