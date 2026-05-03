using GestionCo.Api.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Infrastructure.Services;

public class ReferenceGenerator : IReferenceGenerator
{
    private readonly IAppDbContext _db;

    public ReferenceGenerator(IAppDbContext db) => _db = db;

    public async Task<string> GenerateProductReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PRD-{year}-";

        var lastRef = await _db.Produits
            .Where(p => p.Reference.StartsWith(prefix))
            .OrderByDescending(p => p.Reference)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);

        var nextNum = ExtractNextNumber(lastRef, prefix, padding: 4);
        return $"{prefix}{nextNum:D4}";
    }

    public async Task<string> GenerateSaleReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"VNT-{year}-";

        var lastRef = await _db.Ventes
            .Where(v => v.Reference.StartsWith(prefix))
            .OrderByDescending(v => v.Reference)
            .Select(v => v.Reference)
            .FirstOrDefaultAsync(ct);

        var nextNum = ExtractNextNumber(lastRef, prefix, padding: 4);
        return $"{prefix}{nextNum:D4}";
    }

    public async Task<string> GeneratePurchaseReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"ACH-{year}-";

        var lastRef = await _db.Achats
            .Where(a => a.Reference.StartsWith(prefix))
            .OrderByDescending(a => a.Reference)
            .Select(a => a.Reference)
            .FirstOrDefaultAsync(ct);

        var nextNum = ExtractNextNumber(lastRef, prefix, padding: 4);
        return $"{prefix}{nextNum:D4}";
    }

    public async Task<string> GenerateInvoiceReferenceAsync(CancellationToken ct = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"FAC-{year}-";

        var lastRef = await _db.Factures
            .Where(f => f.NumeroFacture.StartsWith(prefix))
            .OrderByDescending(f => f.NumeroFacture)
            .Select(f => f.NumeroFacture)
            .FirstOrDefaultAsync(ct);

        var nextNum = ExtractNextNumber(lastRef, prefix, padding: 3);
        return $"{prefix}{nextNum:D3}";
    }

    private static int ExtractNextNumber(string? lastRef, string prefix, int padding)
    {
        if (string.IsNullOrEmpty(lastRef)) return 1;
        var numPart = lastRef.Substring(prefix.Length);
        return int.TryParse(numPart, out var num) ? num + 1 : 1;
    }
}
