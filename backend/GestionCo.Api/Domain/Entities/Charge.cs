using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Charge : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Montant { get; set; }
    public decimal MontantPaye { get; set; } = 0;
    public string? Justificatif { get; set; }
    public StatutCharge Statut { get; set; } = StatutCharge.EnAttente;
    public DateTime DateCharge { get; set; } = DateTime.UtcNow;

    public int CategorieChargeId { get; set; }
    public CategorieCharge CategorieCharge { get; set; } = null!;

    public int UtilisateurId { get; set; }
    public Utilisateur Utilisateur { get; set; } = null!;

    public int? FournisseurId { get; set; }
    public Fournisseur? Fournisseur { get; set; }

    public ICollection<PaiementCharge> Paiements { get; set; } = new List<PaiementCharge>();

    // Récurrence
    public bool EstRecurrente { get; set; } = false;
    public string? Periodicite { get; set; } // Mensuelle | Trimestrielle | Annuelle
    public DateTime? DateProchaine { get; set; }

    public decimal Reste => Montant - MontantPaye;
    public bool EstPaye => MontantPaye >= Montant;
    public int ProgressionPaiement => Montant > 0 ? (int)((MontantPaye / Montant) * 100) : 0;
}

public class CategorieCharge
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
}

public class PaiementCharge : AuditableEntity
{
    public int Id { get; set; }

    public int ChargeId { get; set; }
    public Charge Charge { get; set; } = null!;

    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public StatutPaiement Statut { get; set; } = StatutPaiement.Confirme;
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
