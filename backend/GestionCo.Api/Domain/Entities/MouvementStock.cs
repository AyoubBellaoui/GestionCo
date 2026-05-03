using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class MouvementStock : AuditableEntity
{
    public int Id { get; set; }

    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;

    public TypeMouvementStock Type { get; set; }
    public int Quantite { get; set; }

    public int StockAvant { get; set; }
    public int StockApres { get; set; }

    public SourceMouvementStock Source { get; set; }
    public int? ReferenceId { get; set; } // ID de l'achat/vente lié (ou null si manuel)
    public string? ReferenceText { get; set; } // Ex: "VNT-2026-0038"

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public string? Raison { get; set; } // Pour les ajustements manuels
    public string? Commentaire { get; set; }

    public DateTime DateMouvement { get; set; } = DateTime.UtcNow;
}
