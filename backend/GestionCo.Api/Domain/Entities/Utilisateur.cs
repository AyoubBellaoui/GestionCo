using GestionCo.Api.Domain.Common;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Utilisateur : AuditableEntity
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public RoleUtilisateur Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    // Si le user est un client (role = Client), il est lié à un enregistrement Client
    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    // Navigation
    public ICollection<Vente> Ventes { get; set; } = new List<Vente>();
    public ICollection<Log> Logs { get; set; } = new List<Log>();

    public string NomComplet => $"{Prenom} {Nom}".Trim();
    public string Initiales
    {
        get
        {
            var p = string.IsNullOrEmpty(Prenom) ? "" : Prenom[0].ToString();
            var n = string.IsNullOrEmpty(Nom) ? "" : Nom[0].ToString();
            return (p + n).ToUpperInvariant();
        }
    }
}
