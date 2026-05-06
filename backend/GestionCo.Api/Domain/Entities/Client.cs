using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Client : AuditableEntity
{
    public int Id { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public TypeClient Type { get; set; } = TypeClient.Entreprise;

    // Entreprise uniquement
    public string? ICE { get; set; }
    public string? RC { get; set; }
    public string? IF { get; set; }

    // Contact
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? PersonneContact { get; set; }

    public bool IsActive { get; set; } = true;

    // Comment ce client a été acquis (optionnel)
    public string? SourceAcquisition { get; set; }

    // Lien vers utilisateur (optionnel — si client a un accès)
    public int? UtilisateurId { get; set; }
    public Utilisateur? Utilisateur { get; set; }

    // Navigation
    public ICollection<Vente> Ventes { get; set; } = new List<Vente>();

    public string Initiales
    {
        get
        {
            var parts = NomClient.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "??";
            if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpperInvariant() : parts[0].ToUpperInvariant();
            return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant();
        }
    }
}
