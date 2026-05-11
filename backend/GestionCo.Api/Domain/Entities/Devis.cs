using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Devis : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateDevis { get; set; } = DateTime.UtcNow;
    public DateTime? DateValidite { get; set; }

    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; }

    public StatutDevis Statut { get; set; } = StatutDevis.Brouillon;
    public string? Notes { get; set; }

    public int? VenteId { get; set; }
    public Vente? Vente { get; set; }

    public ICollection<LigneDevis> Lignes { get; set; } = new List<LigneDevis>();
}

public class LigneDevis
{
    public int Id { get; set; }

    public int DevisId { get; set; }
    public Devis Devis { get; set; } = null!;

    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;

    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Tva { get; set; } = 20;

    public decimal Total => Quantite * PrixUnitaire;
}
