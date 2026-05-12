using GestionCo.Api.Domain.Common;

namespace GestionCo.Api.Domain.Entities;

public class Produit : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty; // Format : PRD-YYYY-XXXX
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; } // emoji ou URL
    public string? CodeBarre { get; set; }

    // Prix d'achat
    public decimal PrixHT { get; set; }
    public decimal TVA { get; set; } = 20; // 0, 7, 14, 20
    public decimal PrixTTC { get; set; }

    // Prix de vente
    public decimal PrixVenteHT { get; set; }
    public decimal TVAVente { get; set; } = 20;
    public decimal PrixVenteTTC { get; set; }

    // Stock
    public int QuantiteStock { get; set; }
    public int SeuilAlerte { get; set; } = 5;

    // Relations
    public int? CategorieId { get; set; }
    public Categorie? Categorie { get; set; }

    public int? FournisseurId { get; set; }
    public Fournisseur? Fournisseur { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<LigneVente> LignesVente { get; set; } = new List<LigneVente>();
    public ICollection<LigneAchat> LignesAchat { get; set; } = new List<LigneAchat>();
    public ICollection<MouvementStock> MouvementsStock { get; set; } = new List<MouvementStock>();

    public bool IsStockFaible => QuantiteStock > 0 && QuantiteStock <= SeuilAlerte;
    public bool IsRupture => QuantiteStock == 0;
}
