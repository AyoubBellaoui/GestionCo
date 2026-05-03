using GestionCo.Api.Application.Auth.DTOs;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Auth.Commands;

// ============ REFRESH TOKEN ============
public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponse>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IConfiguration _config;

    public RefreshTokenHandler(IAppDbContext db, IJwtService jwt, IConfiguration config)
    {
        _db = db; _jwt = jwt; _config = config;
    }

    public async Task<AuthResponse> Handle(RefreshTokenCommand req, CancellationToken ct)
    {
        var userId = _jwt.ValidateAccessToken(req.AccessToken);
        if (userId == null)
            throw new UnauthorizedException("Token invalide");

        var user = await _db.Utilisateurs
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Id == userId.Value, ct)
            ?? throw new UnauthorizedException("Utilisateur introuvable");

        if (user.RefreshToken != req.RefreshToken ||
            user.RefreshTokenExpiresAt == null ||
            user.RefreshTokenExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token invalide ou expiré");

        var refreshDays = _config.GetValue<int>("JwtSettings:RefreshTokenExpirationDays", 7);
        user.RefreshToken = _jwt.GenerateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshDays);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            AccessToken = _jwt.GenerateAccessToken(user),
            RefreshToken = user.RefreshToken,
            User = new UserDto
            {
                Id = user.Id, Nom = user.Nom, Prenom = user.Prenom,
                Email = user.Email, Role = user.Role, Telephone = user.Telephone,
                ClientId = user.ClientId, NomClient = user.Client?.NomClient,
                IsActive = user.IsActive, LastLoginAt = user.LastLoginAt
            }
        };
    }
}

// ============ LOGOUT ============
public record LogoutCommand : IRequest<Unit>;

public class LogoutHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public LogoutHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<Unit> Handle(LogoutCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) return Unit.Value;

        var user = await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == _current.UserId.Value, ct);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiresAt = null;
            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                ActionLog.Logout, "utilisateurs",
                $"Déconnexion de {user.NomComplet}", user.Id, ct: ct);
        }
        return Unit.Value;
    }
}

// ============ GET CURRENT USER ============
public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public GetCurrentUserHandler(IAppDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var user = await _db.Utilisateurs
            .Include(u => u.Client)
            .FirstOrDefaultAsync(u => u.Id == _current.UserId.Value, ct)
            ?? throw new NotFoundException("Utilisateur", _current.UserId.Value);

        return new UserDto
        {
            Id = user.Id, Nom = user.Nom, Prenom = user.Prenom,
            Email = user.Email, Role = user.Role, Telephone = user.Telephone,
            ClientId = user.ClientId, NomClient = user.Client?.NomClient,
            IsActive = user.IsActive, LastLoginAt = user.LastLoginAt
        };
    }
}
