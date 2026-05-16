using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DevisEntity = GestionCo.Api.Domain.Entities.Devis;

namespace GestionCo.Api.Application.Search;

public record SearchItemDto(
    string Type,
    int    Id,
    string Label,
    string? Sublabel,
    string? Badge,
    string Route,
    string Icon
);

public record SearchResultsDto(
    List<SearchItemDto> Clients,
    List<SearchItemDto> Produits,
    List<SearchItemDto> Ventes,
    List<SearchItemDto> Factures,
    List<SearchItemDto> Devis,
    List<SearchItemDto> Achats,
    List<SearchItemDto> Fournisseurs,
    int Total
);

public record GlobalSearchQuery(string Q, int Take = 5) : IRequest<SearchResultsDto>;

public class GlobalSearchHandler : IRequestHandler<GlobalSearchQuery, SearchResultsDto>
{
    private readonly IAppDbContext _db;
    public GlobalSearchHandler(IAppDbContext db) => _db = db;

    public async Task<SearchResultsDto> Handle(GlobalSearchQuery q, CancellationToken ct)
    {
        var term = q.Q.Trim();
        if (term.Length < 2)
            return new SearchResultsDto([], [], [], [], [], [], [], 0);

        var take = Math.Clamp(q.Take, 1, 10);

        var clients = await _db.Clients
            .Where(c => c.IsActive && (
                c.NomClient.Contains(term) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.Telephone != null && c.Telephone.Contains(term)) ||
                (c.ICE != null && c.ICE.Contains(term))
            ))
            .Take(take)
            .Select(c => new SearchItemDto("client", c.Id, c.NomClient,
                c.Email ?? c.Telephone ?? c.Ville, null, "/clients", "👥"))
            .ToListAsync(ct);

        var produits = await _db.Produits
            .Where(p => p.IsActive && (p.Nom.Contains(term) || p.Reference.Contains(term)))
            .Take(take)
            .Select(p => new SearchItemDto("produit", p.Id, p.Nom,
                p.Reference, null, "/produits", "📦"))
            .ToListAsync(ct);

        var ventes = await _db.Ventes
            .Where(v => v.Statut != StatutVente.Annule && (
                v.Reference.Contains(term) || v.Client.NomClient.Contains(term)
            ))
            .OrderByDescending(v => v.DateVente)
            .Take(take)
            .Select(v => new SearchItemDto("vente", v.Id, v.Reference,
                v.Client.NomClient, v.Statut.ToString(), "/ventes", "🛒"))
            .ToListAsync(ct);

        var factures = await _db.Factures
            .Where(f => f.NumeroFacture.Contains(term) || f.Vente.Client.NomClient.Contains(term))
            .OrderByDescending(f => f.DateEmission)
            .Take(take)
            .Select(f => new SearchItemDto("facture", f.Id, f.NumeroFacture,
                f.Vente.Client.NomClient, f.Statut.ToString(), "/factures", "🧾"))
            .ToListAsync(ct);

        var devis = await _db.Devis
            .Where(d => d.Reference.Contains(term) || d.Client.NomClient.Contains(term))
            .OrderByDescending(d => d.DateDevis)
            .Take(take)
            .Select(d => new SearchItemDto("devis", d.Id, d.Reference,
                d.Client.NomClient, d.Statut.ToString(), "/devis", "📝"))
            .ToListAsync(ct);

        var achats = await _db.Achats
            .Where(a => a.Reference.Contains(term) || a.Fournisseur.Nom.Contains(term))
            .OrderByDescending(a => a.DateAchat)
            .Take(take)
            .Select(a => new SearchItemDto("achat", a.Id, a.Reference,
                a.Fournisseur.Nom, a.Statut.ToString(), "/achats", "📥"))
            .ToListAsync(ct);

        var fournisseurs = await _db.Fournisseurs
            .Where(f => f.IsActive && f.Nom.Contains(term))
            .Take(take)
            .Select(f => new SearchItemDto("fournisseur", f.Id, f.Nom,
                f.Email ?? f.Telephone, null, "/fournisseurs", "🚚"))
            .ToListAsync(ct);

        var total = clients.Count + produits.Count + ventes.Count +
                    factures.Count + devis.Count + achats.Count + fournisseurs.Count;

        return new SearchResultsDto(clients, produits, ventes, factures, devis, achats, fournisseurs, total);
    }
}
