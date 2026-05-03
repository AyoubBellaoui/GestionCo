using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Achats;

public class AchatDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int FournisseurId { get; set; }
    public string NomFournisseur { get; set; } = string.Empty;
    public string? IconeFournisseur { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public DateTime DateAchat { get; set; }
    public decimal MontantTotal { get; set; }
    public string? Notes { get; set; }
    public int NombreArticles { get; set; }
    public List<LigneAchatDto> Lignes { get; set; } = new();
}

public class LigneAchatDto
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string ReferenceProduit { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Total { get; set; }
}

public class CreateAchatDto
{
    public int FournisseurId { get; set; }
    public DateTime? DateAchat { get; set; }
    public string? Notes { get; set; }
    public List<CreateLigneAchatDto> Lignes { get; set; } = new();
}

public class CreateLigneAchatDto
{
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
}

// ============ COMMANDS ============
public record CreateAchatCommand(CreateAchatDto Dto) : IRequest<AchatDto>;

public class CreateAchatValidator : AbstractValidator<CreateAchatCommand>
{
    public CreateAchatValidator()
    {
        RuleFor(x => x.Dto.FournisseurId).GreaterThan(0);
        RuleFor(x => x.Dto.Lignes).NotEmpty();
        RuleForEach(x => x.Dto.Lignes).ChildRules(l =>
        {
            l.RuleFor(x => x.ProduitId).GreaterThan(0);
            l.RuleFor(x => x.Quantite).GreaterThan(0);
            l.RuleFor(x => x.PrixUnitaire).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateAchatHandler : IRequestHandler<CreateAchatCommand, AchatDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public CreateAchatHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit;
    }

    public async Task<AchatDto> Handle(CreateAchatCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var fournisseur = await _db.Fournisseurs.FirstOrDefaultAsync(f => f.Id == dto.FournisseurId, ct)
            ?? throw new NotFoundException("Fournisseur", dto.FournisseurId);

        var produitIds = dto.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits.Where(p => produitIds.Contains(p.Id)).ToListAsync(ct);

        foreach (var ligne in dto.Lignes)
        {
            if (!produits.Any(p => p.Id == ligne.ProduitId))
                throw new NotFoundException("Produit", ligne.ProduitId);
        }

        var reference = await _refGen.GeneratePurchaseReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var achat = new Achat
        {
            Reference = reference,
            FournisseurId = dto.FournisseurId,
            UtilisateurId = userId,
            DateAchat = dto.DateAchat ?? now,
            Notes = dto.Notes,
            Lignes = dto.Lignes.Select(l => new LigneAchat
            {
                ProduitId = l.ProduitId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire
            }).ToList()
        };
        achat.MontantTotal = achat.Lignes.Sum(l => l.Quantite * l.PrixUnitaire);

        _db.Achats.Add(achat);
        await _db.SaveChangesAsync(ct);

        // Incrémenter stock + mouvements
        foreach (var ligne in achat.Lignes)
        {
            var produit = produits.First(p => p.Id == ligne.ProduitId);
            var stockAvant = produit.QuantiteStock;
            produit.QuantiteStock += ligne.Quantite;

            _db.MouvementsStock.Add(new MouvementStock
            {
                ProduitId = produit.Id,
                Type = TypeMouvementStock.Entree,
                Quantite = ligne.Quantite,
                StockAvant = stockAvant,
                StockApres = produit.QuantiteStock,
                Source = SourceMouvementStock.Achat,
                ReferenceId = achat.Id,
                ReferenceText = achat.Reference,
                UtilisateurId = userId,
                DateMouvement = now
            });
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "achats",
            $"Achat créé : {achat.Reference} chez {fournisseur.Nom} ({achat.MontantTotal:N2} MAD)",
            achat.Id, achat.Reference, ct: ct);

        return await GetAchatDetails(achat.Id, ct);
    }

    private async Task<AchatDto> GetAchatDetails(int id, CancellationToken ct)
    {
        var a = await _db.Achats
            .Include(a => a.Fournisseur).Include(a => a.Utilisateur)
            .Include(a => a.Lignes).ThenInclude(l => l.Produit)
            .FirstAsync(a => a.Id == id, ct);
        return AchatMapper.ToDto(a);
    }
}

// ============ QUERIES ============
public record GetAchatsQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    int? FournisseurId = null,
    DateTime? DateDebut = null,
    DateTime? DateFin = null
) : IRequest<PagedList<AchatDto>>;

public class GetAchatsHandler : IRequestHandler<GetAchatsQuery, PagedList<AchatDto>>
{
    private readonly IAppDbContext _db;
    public GetAchatsHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<AchatDto>> Handle(GetAchatsQuery q, CancellationToken ct)
    {
        var query = _db.Achats
            .Include(a => a.Fournisseur)
            .Include(a => a.Utilisateur)
            .Include(a => a.Lignes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(a =>
                a.Reference.ToLower().Contains(s) ||
                a.Fournisseur.Nom.ToLower().Contains(s));
        }

        if (q.FournisseurId.HasValue) query = query.Where(a => a.FournisseurId == q.FournisseurId);
        if (q.DateDebut.HasValue) query = query.Where(a => a.DateAchat >= q.DateDebut);
        if (q.DateFin.HasValue) query = query.Where(a => a.DateAchat <= q.DateFin);

        query = query.OrderByDescending(a => a.DateAchat);

        var paged = await PagedList<Achat>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<AchatDto>
        {
            Items = paged.Items.Select(AchatMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount, Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public record GetAchatByIdQuery(int Id) : IRequest<AchatDto>;

public class GetAchatByIdHandler : IRequestHandler<GetAchatByIdQuery, AchatDto>
{
    private readonly IAppDbContext _db;
    public GetAchatByIdHandler(IAppDbContext db) => _db = db;

    public async Task<AchatDto> Handle(GetAchatByIdQuery q, CancellationToken ct)
    {
        var a = await _db.Achats
            .Include(a => a.Fournisseur).Include(a => a.Utilisateur)
            .Include(a => a.Lignes).ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(a => a.Id == q.Id, ct)
            ?? throw new NotFoundException("Achat", q.Id);
        return AchatMapper.ToDto(a);
    }
}

public static class AchatMapper
{
    public static AchatDto ToDto(Achat a) => new()
    {
        Id = a.Id, Reference = a.Reference,
        FournisseurId = a.FournisseurId,
        NomFournisseur = a.Fournisseur?.Nom ?? "",
        IconeFournisseur = a.Fournisseur?.Icone,
        UtilisateurId = a.UtilisateurId,
        NomUtilisateur = a.Utilisateur?.NomComplet ?? "",
        DateAchat = a.DateAchat, MontantTotal = a.MontantTotal,
        Notes = a.Notes,
        NombreArticles = a.Lignes?.Sum(l => l.Quantite) ?? 0,
        Lignes = a.Lignes?.Select(l => new LigneAchatDto
        {
            Id = l.Id, ProduitId = l.ProduitId,
            NomProduit = l.Produit?.Nom ?? "",
            ReferenceProduit = l.Produit?.Reference ?? "",
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Total = l.Total
        }).ToList() ?? new()
    };
}
