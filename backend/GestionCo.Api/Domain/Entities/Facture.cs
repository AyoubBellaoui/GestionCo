using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Facture : AuditableEntity
{
    public int Id { get; set; }
    public string NumeroFacture { get; set; } = string.Empty; // Format : FAC-YYYY-XXX

    public int VenteId { get; set; }
    public Vente Vente { get; set; } = null!;

    public DateTime DateEmission { get; set; } = DateTime.UtcNow;
    public DateTime DateEcheance { get; set; }

    public StatutFacture Statut { get; set; } = StatutFacture.EnAttente;
    public bool EstEnvoyeeEmail { get; set; } = false;
    public DateTime? DateEnvoiEmail { get; set; }
}
