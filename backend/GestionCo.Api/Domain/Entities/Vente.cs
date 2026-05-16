using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Vente : AuditableEntity
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty; // Format : VNT-YYYY-XXXX

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public int UtilisateurId { get; set; } // Qui a créé la vente
    public Utilisateur Utilisateur { get; set; } = null!;

    public DateTime DateVente { get; set; } = DateTime.UtcNow;
    public DateTime? DateEcheance { get; set; }

    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; } // TTC
    public decimal MontantPaye { get; set; } = 0;

    public StatutVente Statut { get; set; } = StatutVente.EnAttente;

    // Navigation
    public ICollection<LigneVente> Lignes { get; set; } = new List<LigneVente>();
    public ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();
    public Facture? Facture { get; set; }

    public decimal Reste => MontantTotal - MontantPaye;
    public bool EstPayee => MontantPaye >= MontantTotal;
    public int ProgressionPaiement => MontantTotal > 0 ? (int)((MontantPaye / MontantTotal) * 100) : 0;
}

public class LigneVente
{
    public int Id { get; set; }

    public int VenteId { get; set; }
    public Vente Vente { get; set; } = null!;

    public int ProduitId { get; set; }
    public Produit Produit { get; set; } = null!;

    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; } = 0;
    public decimal TVA { get; set; } = 20;
    public decimal Total => Quantite * PrixUnitaire * (1 - Remise / 100);
}
