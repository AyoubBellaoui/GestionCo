using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Log
{
    public int Id { get; set; }

    public int? UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    public ActionLog Action { get; set; }
    public string Entite { get; set; } = string.Empty; // Nom de la table/entité
    public string? EntiteReference { get; set; } // PRD-2026-0042, VNT-2026-0038...
    public int? EntiteId { get; set; }
    public string Description { get; set; } = string.Empty;

    public string? AnciennesValeurs { get; set; } // JSON
    public string? NouvellesValeurs { get; set; } // JSON

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public bool EstSensible { get; set; } = false; // Ajustement manuel, suppression critique...

    public DateTime DateAction { get; set; } = DateTime.UtcNow;
}
