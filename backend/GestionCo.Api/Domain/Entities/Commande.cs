using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Commande : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int? DevisId { get; set; }
    public Devis? Devis { get; set; }

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateCommande { get; set; } = DateTime.UtcNow;
    public DateTime? DateLivraison { get; set; }

    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; }

    public StatutCommande Statut { get; set; } = StatutCommande.EnAttente;
    public string? Notes { get; set; }

    public int? VenteId { get; set; }
    public Vente? Vente { get; set; }

    public ICollection<LigneCommande> Lignes { get; set; } = new List<LigneCommande>();
}

public class LigneCommande
{
    public int Id { get; set; }

    public int CommandeId { get; set; }
    public Commande Commande { get; set; } = null!;

    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;

    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; } = 0;
    public decimal Tva { get; set; } = 20;

    public decimal Total => Quantite * PrixUnitaire * (1 - Remise / 100);
}
