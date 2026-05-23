using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Infrastructure.Services;

public class ReferenceGenerator : IReferenceGenerator
{
    private readonly IAppDbContext _db;

    public ReferenceGenerator(IAppDbContext db) => _db = db;

    private async Task<ParametresFacturation> GetParamsAsync(CancellationToken ct)
        => await _db.ParametresFacturation.AsNoTracking().FirstOrDefaultAsync(ct)
           ?? new ParametresFacturation();

    private static string BuildPrefix(string basePrefix, bool includeAnnee)
        => includeAnnee ? $"{basePrefix}-{DateTime.UtcNow.Year}-" : $"{basePrefix}-";

    public async Task<string> GenerateProductReferenceAsync(CancellationToken ct = default)
    {
        var pf = await GetParamsAsync(ct);
        var prefix = BuildPrefix(pf.PrefixeProduit, pf.IncludeAnnee);

        var lastRef = await _db.Produits
            .Where(p => p.Reference.StartsWith(prefix))
            .OrderByDescending(p => p.Reference)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    public async Task<string> GenerateSaleReferenceAsync(CancellationToken ct = default)
    {
        var pf = await GetParamsAsync(ct);
        var prefix = BuildPrefix(pf.PrefixeVente, pf.IncludeAnnee);

        var lastRef = await _db.Ventes
            .Where(v => v.Reference.StartsWith(prefix))
            .OrderByDescending(v => v.Reference)
            .Select(v => v.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    public async Task<string> GeneratePurchaseReferenceAsync(CancellationToken ct = default)
    {
        var pf = await GetParamsAsync(ct);
        var prefix = BuildPrefix(pf.PrefixeAchat, pf.IncludeAnnee);

        var lastRef = await _db.Achats
            .Where(a => a.Reference.StartsWith(prefix))
            .OrderByDescending(a => a.Reference)
            .Select(a => a.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    public async Task<string> GenerateInvoiceReferenceAsync(CancellationToken ct = default)
    {
        var pf = await GetParamsAsync(ct);
        var prefix = BuildPrefix(pf.PrefixeFacture, pf.IncludeAnnee);

        var lastRef = await _db.Factures
            .Where(f => f.NumeroFacture.StartsWith(prefix))
            .OrderByDescending(f => f.NumeroFacture)
            .Select(f => f.NumeroFacture)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 3):D3}";
    }

    public async Task<string> GenerateChargeReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CHG-{year}-";

        var lastRef = await _db.Charges
            .Where(c => c.Reference.StartsWith(prefix))
            .OrderByDescending(c => c.Reference)
            .Select(c => c.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    public async Task<string> GenerateDevisReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"DVS-{year}-";

        var lastRef = await _db.Devis
            .Where(d => d.Reference.StartsWith(prefix))
            .OrderByDescending(d => d.Reference)
            .Select(d => d.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    public async Task<string> GenerateCommandeReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CMD-{year}-";

        var lastRef = await _db.Commandes
            .Where(c => c.Reference.StartsWith(prefix))
            .OrderByDescending(c => c.Reference)
            .Select(c => c.Reference)
            .FirstOrDefaultAsync(ct);

        return $"{prefix}{ExtractNextNumber(lastRef, prefix, 4):D4}";
    }

    private static int ExtractNextNumber(string? lastRef, string prefix, int padding)
    {
        if (string.IsNullOrEmpty(lastRef)) return 1;
        var numPart = lastRef.Substring(prefix.Length);
        return int.TryParse(numPart, out var num) ? num + 1 : 1;
    }
}
