using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using System.Text.Json;

namespace GestionCo.Api.Infrastructure.Logging;

public class AuditLogger : IAuditLogger
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditLogger(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task LogAsync(
        ActionLog action,
        string entite,
        string description,
        int? entiteId = null,
        string? entiteReference = null,
        object? anciennesValeurs = null,
        object? nouvellesValeurs = null,
        bool estSensible = false,
        CancellationToken ct = default,
        int? utilisateurId = null)
    {
        var log = new Log
        {
            UtilisateurId = utilisateurId ?? _currentUser.UserId,
            Action = action,
            Entite = entite,
            EntiteId = entiteId,
            EntiteReference = entiteReference,
            Description = description,
            AnciennesValeurs = anciennesValeurs != null ? JsonSerializer.Serialize(anciennesValeurs) : null,
            NouvellesValeurs = nouvellesValeurs != null ? JsonSerializer.Serialize(nouvellesValeurs) : null,
            IpAddress = _currentUser.IpAddress,
            UserAgent = _currentUser.UserAgent,
            EstSensible = estSensible,
            DateAction = DateTime.UtcNow
        };

        _db.Logs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
