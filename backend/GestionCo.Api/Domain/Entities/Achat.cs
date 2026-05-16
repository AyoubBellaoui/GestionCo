using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Achat : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;

    public int FournisseurId { get; set; }
    public Fournisseur Fournisseur { get; set; } = null!;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateAchat { get; set; } = DateTime.UtcNow;
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; } = 0;
    public string? Notes { get; set; }
    public StatutAchat Statut { get; set; } = StatutAchat.EnAttente;

    public ICollection<LigneAchat> Lignes { get; set; } = new List<LigneAchat>();
    public ICollection<PaiementAchat> PaiementsAchat { get; set; } = new List<PaiementAchat>();

    public decimal Reste => MontantTotal - MontantPaye;
    public bool EstPaye => MontantPaye >= MontantTotal;
    public int ProgressionPaiement => MontantTotal > 0 ? (int)((MontantPaye / MontantTotal) * 100) : 0;
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
    public decimal Remise { get; set; } = 0;
    public decimal Total => Quantite * PrixUnitaire * (1 - Remise / 100);
}

public class PaiementAchat : AuditableEntity
{
    public int Id { get; set; }

    public int AchatId { get; set; }
    public Achat Achat { get; set; } = null!;

    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public StatutPaiement Statut { get; set; } = StatutPaiement.Confirme;
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
