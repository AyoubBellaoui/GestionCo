using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Application.Produits.Commands;
using GestionCo.Api.Application.Produits.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Produits.Queries;

// ============ GET ALL (avec pagination, filtre, tri) ============
public record GetProduitsQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    int? CategorieId = null,
    int? FournisseurId = null,
    bool? StockFaibleOnly = null,
    bool? RuptureOnly = null,
    bool? DisponibleOnly = null,
    string? SortBy = null,
    bool SortDesc = true
) : IRequest<PagedList<ProduitDto>>;

public class GetProduitsHandler : IRequestHandler<GetProduitsQuery, PagedList<ProduitDto>>
{
    private readonly IAppDbContext _db;

    public GetProduitsHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<ProduitDto>> Handle(GetProduitsQuery q, CancellationToken ct)
    {
        var query = _db.Produits
            .Include(p => p.Categorie)
            .Include(p => p.Fournisseur)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(p =>
                p.Nom.ToLower().Contains(s) ||
                p.Reference.ToLower().Contains(s) ||
                (p.CodeBarre != null && p.CodeBarre.Contains(s)));
        }

        if (q.CategorieId.HasValue)
            query = query.Where(p => p.CategorieId == q.CategorieId);

        if (q.FournisseurId.HasValue)
            query = query.Where(p => p.FournisseurId == q.FournisseurId);

        if (q.StockFaibleOnly == true)
            query = query.Where(p => p.QuantiteStock > 0 && p.QuantiteStock <= p.SeuilAlerte);

        if (q.RuptureOnly == true)
            query = query.Where(p => p.QuantiteStock == 0);

        if (q.DisponibleOnly == true)
            query = query.Where(p => p.QuantiteStock > p.SeuilAlerte);

        // Tri
        query = (q.SortBy?.ToLower(), q.SortDesc) switch
        {
            ("nom", true) => query.OrderByDescending(p => p.Nom),
            ("nom", false) => query.OrderBy(p => p.Nom),
            ("prix", true) => query.OrderByDescending(p => p.PrixHT),
            ("prix", false) => query.OrderBy(p => p.PrixHT),
            ("stock", true) => query.OrderByDescending(p => p.QuantiteStock),
            ("stock", false) => query.OrderBy(p => p.QuantiteStock),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var paged = await PagedList<Domain.Entities.Produit>.CreateAsync(query, q.Page, q.PageSize, ct);

        return new PagedList<ProduitDto>
        {
            Items = paged.Items.Select(ProduitMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}

// ============ GET BY ID ============
public record GetProduitByIdQuery(int Id) : IRequest<ProduitDto>;

public class GetProduitByIdHandler : IRequestHandler<GetProduitByIdQuery, ProduitDto>
{
    private readonly IAppDbContext _db;
    public GetProduitByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ProduitDto> Handle(GetProduitByIdQuery q, CancellationToken ct)
    {
        var p = await _db.Produits
            .Include(p => p.Categorie).Include(p => p.Fournisseur)
            .FirstOrDefaultAsync(p => p.Id == q.Id, ct)
            ?? throw new NotFoundException("Produit", q.Id);
        return ProduitMapper.ToDto(p);
    }
}

// ============ STATS PRODUITS ============
public record GetProduitsStatsQuery : IRequest<ProduitsStatsDto>;

public class ProduitsStatsDto
{
    public int TotalProduits { get; set; }
    public int ProduitsActifs { get; set; }
    public int ProduitsStockFaible { get; set; }
    public int ProduitsRupture { get; set; }
    public decimal ValeurTotaleStock { get; set; }
}

public class GetProduitsStatsHandler : IRequestHandler<GetProduitsStatsQuery, ProduitsStatsDto>
{
    private readonly IAppDbContext _db;
    public GetProduitsStatsHandler(IAppDbContext db) => _db = db;

    public async Task<ProduitsStatsDto> Handle(GetProduitsStatsQuery q, CancellationToken ct)
    {
        var total = await _db.Produits.CountAsync(ct);
        var actifs = await _db.Produits.CountAsync(p => p.IsActive, ct);
        var stockFaible = await _db.Produits.CountAsync(p => p.QuantiteStock > 0 && p.QuantiteStock <= p.SeuilAlerte, ct);
        var rupture = await _db.Produits.CountAsync(p => p.QuantiteStock == 0, ct);
        var valeur = await _db.Produits.SumAsync(p => p.PrixHT * p.QuantiteStock, ct);

        return new ProduitsStatsDto
        {
            TotalProduits = total,
            ProduitsActifs = actifs,
            ProduitsStockFaible = stockFaible,
            ProduitsRupture = rupture,
            ValeurTotaleStock = valeur
        };
    }
}
