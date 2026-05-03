using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.MouvementsStock;

public class MouvementStockDto
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string ReferenceProduit { get; set; } = string.Empty;
    public string? ImageProduit { get; set; }
    public TypeMouvementStock Type { get; set; }
    public int Quantite { get; set; }
    public int StockAvant { get; set; }
    public int StockApres { get; set; }
    public SourceMouvementStock Source { get; set; }
    public string SourceLibelle => Source.ToString();
    public string? ReferenceText { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public string? Raison { get; set; }
    public string? Commentaire { get; set; }
    public DateTime DateMouvement { get; set; }
}

public class AjustementStockDto
{
    public int ProduitId { get; set; }
    public TypeMouvementStock Type { get; set; }
    public int Quantite { get; set; }
    public string Raison { get; set; } = string.Empty;
    public string? Commentaire { get; set; }
}

public record CreateAjustementStockCommand(AjustementStockDto Dto) : IRequest<MouvementStockDto>;

public class AjustementValidator : AbstractValidator<CreateAjustementStockCommand>
{
    public AjustementValidator()
    {
        RuleFor(x => x.Dto.ProduitId).GreaterThan(0);
        RuleFor(x => x.Dto.Quantite).GreaterThan(0);
        RuleFor(x => x.Dto.Raison).NotEmpty();
    }
}

public class CreateAjustementStockHandler : IRequestHandler<CreateAjustementStockCommand, MouvementStockDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public CreateAjustementStockHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<MouvementStockDto> Handle(CreateAjustementStockCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var produit = await _db.Produits.FirstOrDefaultAsync(p => p.Id == dto.ProduitId, ct)
            ?? throw new NotFoundException("Produit", dto.ProduitId);

        var stockAvant = produit.QuantiteStock;
        var nouvelleQte = dto.Type == TypeMouvementStock.Entree
            ? produit.QuantiteStock + dto.Quantite
            : produit.QuantiteStock - dto.Quantite;

        if (nouvelleQte < 0)
            throw new BusinessException(
                $"Stock insuffisant. Stock actuel : {produit.QuantiteStock}, demandé : {dto.Quantite}");

        produit.QuantiteStock = nouvelleQte;

        var mouvement = new MouvementStock
        {
            ProduitId = produit.Id,
            Type = dto.Type,
            Quantite = dto.Quantite,
            StockAvant = stockAvant,
            StockApres = nouvelleQte,
            Source = SourceMouvementStock.Manuel,
            UtilisateurId = userId,
            Raison = dto.Raison,
            Commentaire = dto.Commentaire,
            DateMouvement = DateTime.UtcNow
        };

        _db.MouvementsStock.Add(mouvement);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            ActionLog.Sensitive, "mouvements_stock",
            $"Ajustement manuel {(dto.Type == TypeMouvementStock.Entree ? "+" : "-")}{dto.Quantite} sur {produit.Nom} — Raison: {dto.Raison}",
            mouvement.Id, produit.Reference,
            anciennesValeurs: new { stock = stockAvant },
            nouvellesValeurs: new { stock = nouvelleQte, raison = dto.Raison },
            estSensible: true, ct: ct);

        return await GetDetails(mouvement.Id, ct);
    }

    private async Task<MouvementStockDto> GetDetails(int id, CancellationToken ct)
    {
        var m = await _db.MouvementsStock
            .Include(m => m.Produit).Include(m => m.Utilisateur)
            .FirstAsync(m => m.Id == id, ct);
        return MouvementStockMapper.ToDto(m);
    }
}

public record GetMouvementsStockQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    TypeMouvementStock? Type = null,
    SourceMouvementStock? Source = null,
    int? ProduitId = null
) : IRequest<PagedList<MouvementStockDto>>;

public class GetMouvementsStockHandler : IRequestHandler<GetMouvementsStockQuery, PagedList<MouvementStockDto>>
{
    private readonly IAppDbContext _db;
    public GetMouvementsStockHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<MouvementStockDto>> Handle(GetMouvementsStockQuery q, CancellationToken ct)
    {
        var query = _db.MouvementsStock
            .Include(m => m.Produit).Include(m => m.Utilisateur)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(m => m.Produit.Nom.ToLower().Contains(s));
        }
        if (q.Type.HasValue) query = query.Where(m => m.Type == q.Type);
        if (q.Source.HasValue) query = query.Where(m => m.Source == q.Source);
        if (q.ProduitId.HasValue) query = query.Where(m => m.ProduitId == q.ProduitId);

        query = query.OrderByDescending(m => m.DateMouvement);

        var paged = await PagedList<MouvementStock>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<MouvementStockDto>
        {
            Items = paged.Items.Select(MouvementStockMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public static class MouvementStockMapper
{
    public static MouvementStockDto ToDto(MouvementStock m) => new()
    {
        Id = m.Id, ProduitId = m.ProduitId,
        NomProduit = m.Produit?.Nom ?? "",
        ReferenceProduit = m.Produit?.Reference ?? "",
        ImageProduit = m.Produit?.Image,
        Type = m.Type, Quantite = m.Quantite,
        StockAvant = m.StockAvant, StockApres = m.StockApres,
        Source = m.Source, ReferenceText = m.ReferenceText,
        UtilisateurId = m.UtilisateurId,
        NomUtilisateur = m.Utilisateur?.NomComplet ?? "",
        Raison = m.Raison, Commentaire = m.Commentaire,
        DateMouvement = m.DateMouvement
    };
}
