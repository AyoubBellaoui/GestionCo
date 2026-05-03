using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Application.Auth.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public RoleUtilisateur Role { get; set; }
    public string RoleLibelle => Role.ToString();
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
    public int? ClientId { get; set; }
    public string? NomClient { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
