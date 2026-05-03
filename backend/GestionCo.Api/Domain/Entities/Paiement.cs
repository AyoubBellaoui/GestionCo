using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Paiement : AuditableEntity
{
    public int Id { get; set; }

    public int VenteId { get; set; }
    public Vente Vente { get; set; } = null!;

    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public StatutPaiement Statut { get; set; } = StatutPaiement.Confirme;
    public DateTime DatePaiement { get; set; } = DateTime.UtcNow;
    public string? Reference { get; set; } // N° transaction, virement, etc.
    public string? Notes { get; set; }
}
