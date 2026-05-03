using GestionCo.Api.Domain.Common;

namespace GestionCo.Api.Domain.Entities;

public class Achat : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty; // Format : ACH-YYYY-XXXX

    public int FournisseurId { get; set; }
    public Fournisseur Fournisseur { get; set; } = null!;

    public int UtilisateurId { get; set; } // Qui a saisi l'achat
    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateAchat { get; set; } = DateTime.UtcNow;
    public decimal MontantTotal { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<LigneAchat> Lignes { get; set; } = new List<LigneAchat>();
}

public class LigneAchat
{
    public int Id { get; set; }

    public int AchatId { get; set; }
    public Achat Achat { get; set; } = null!;

    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;

    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Total => Quantite * PrixUnitaire;
}
