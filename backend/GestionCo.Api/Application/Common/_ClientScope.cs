using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Common.Security;

// Espace client : un utilisateur de rôle Client ne voit que les documents de son propre client
public static class ClientScope
{
    // null = aucune restriction (Admin, Gestionnaire, lien de partage anonyme).
    // Sinon : id du client rattaché au compte, ou 0 si aucun client n'est rattaché (aucun document visible).
    public static async Task<int?> GetClientIdAsync(IAppDbContext db, ICurrentUserService current, CancellationToken ct)
    {
        if (current.Role != RoleUtilisateur.Client) return null;
        if (!current.UserId.HasValue) return 0;

        var clientId = await db.Utilisateurs
            .Where(u => u.Id == current.UserId.Value)
            .Select(u => u.ClientId)
            .FirstOrDefaultAsync(ct);
        return clientId ?? 0;
    }
}
