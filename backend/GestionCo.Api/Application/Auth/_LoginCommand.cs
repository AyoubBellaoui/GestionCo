using FluentValidation;
using GestionCo.Api.Application.Auth.DTOs;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress()
            .WithMessage("Email invalide");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6)
            .WithMessage("Mot de passe requis (min 6 caractères)");
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IAuditLogger _audit;
    private readonly IConfiguration _config;

    public LoginHandler(
        IAppDbContext db, IPasswordHasher hasher,
        IJwtService jwt, IAuditLogger audit, IConfiguration config)
    {
        _db = db; _hasher = hasher; _jwt = jwt; _audit = audit; _config = config;
    }

    public async Task<AuthResponse> Handle(LoginCommand req, CancellationToken ct)
    {
        var user = await _db.Utilisateurs
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower().Trim(), ct);

        if (user == null || !_hasher.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedException("Email ou mot de passe incorrect");

        if (!user.IsActive)
            throw new UnauthorizedException("Compte désactivé. Contactez un administrateur.");

        var refreshDays = _config.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);

        user.RefreshToken = _jwt.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
        user.LastLoginAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            ActionLog.Login,
            "utilisateurs",
            $"Connexion réussie de {user.NomComplet}",
            user.Id,
            ct: ct);

        return new AuthResponse
        {
            AccessToken = _jwt.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Nom = user.Nom,
                Prenom = user.Prenom,
                Email = user.Email,
                Telephone = user.Telephone,
                Role = user.Role,
                ClientId = user.ClientId,
                NomClient = user.Client?.NomClient,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            }
        };
    }
}
