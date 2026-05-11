using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Achats;

// ============ DTOs ============
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
    public decimal MontantPaye { get; set; }
    public decimal Reste { get; set; }
    public int ProgressionPaiement { get; set; }
    public StatutAchat Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
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
    public decimal? PaiementInitial { get; set; }
    public MethodePaiement? MethodePaiementInitial { get; set; }
}

public class CreateLigneAchatDto
{
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
}

public class AddPaiementAchatDto
{
    public int AchatId { get; set; }
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; } = MethodePaiement.Espece;
    public string? Notes { get; set; }
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

public record AddPaiementAchatCommand(AddPaiementAchatDto Dto) : IRequest<AchatDto>;

// ============ HANDLERS ============
public class CreateAchatHandler : IRequestHandler<CreateAchatCommand, AchatDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public CreateAchatHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit; _notif = notif;
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

        // Paiement initial
        var paiementInitial = dto.PaiementInitial ?? 0;
        if (paiementInitial > 0)
        {
            paiementInitial = Math.Min(paiementInitial, achat.MontantTotal);
            achat.MontantPaye = paiementInitial;
            achat.Statut = paiementInitial >= achat.MontantTotal ? StatutAchat.Paye : StatutAchat.Partiel;
            achat.PaiementsAchat.Add(new PaiementAchat
            {
                Montant = paiementInitial,
                Methode = dto.MethodePaiementInitial ?? MethodePaiement.Espece,
                Statut = StatutPaiement.Confirme,
                DatePaiement = now
            });
        }

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

        await _notif.CreateAsync(
            titre: $"Nouvel achat — {achat.Reference}",
            message: $"Achat de {achat.MontantTotal:N2} MAD passé chez {fournisseur.Nom}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Achat,
            entiteId: achat.Id, entiteReference: achat.Reference,
            lienUrl: "/achats", ct: ct);

        return await GetAchatDetails(achat.Id, ct);
    }

    private async Task<AchatDto> GetAchatDetails(int id, CancellationToken ct)
    {
        var a = await _db.Achats
            .Include(a => a.Fournisseur).Include(a => a.Utilisateur)
            .Include(a => a.Lignes).ThenInclude(l => l.Produit)
            .Include(a => a.PaiementsAchat)
            .FirstAsync(a => a.Id == id, ct);
        return AchatMapper.ToDto(a);
    }
}

public class AddPaiementAchatHandler : IRequestHandler<AddPaiementAchatCommand, AchatDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public AddPaiementAchatHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<AchatDto> Handle(AddPaiementAchatCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var achat = await _db.Achats
            .Include(a => a.PaiementsAchat)
            .Include(a => a.Fournisseur)
            .FirstOrDefaultAsync(a => a.Id == dto.AchatId, ct)
            ?? throw new NotFoundException("Achat", dto.AchatId);

        var montant = Math.Min(dto.Montant, achat.Reste);
        if (montant <= 0) throw new BusinessException("Montant invalide ou achat déjà soldé");

        achat.PaiementsAchat.Add(new PaiementAchat
        {
            AchatId = achat.Id,
            Montant = montant,
            Methode = dto.Methode,
            Statut = StatutPaiement.Confirme,
            DatePaiement = DateTime.UtcNow,
            Notes = dto.Notes
        });

        achat.MontantPaye += montant;
        achat.Statut = achat.MontantPaye >= achat.MontantTotal ? StatutAchat.Paye : StatutAchat.Partiel;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "achats",
            $"Paiement de {montant:N2} MAD ajouté à {achat.Reference}",
            achat.Id, achat.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Paiement fournisseur — {achat.Reference}",
            message: $"Paiement de {montant:N2} MAD envoyé à {achat.Fournisseur?.Nom ?? "fournisseur"} pour {achat.Reference}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Paiement,
            entiteId: achat.Id, entiteReference: achat.Reference,
            lienUrl: "/achats", ct: ct);

        return await GetAchatDetails(achat.Id, ct);
    }

    private async Task<AchatDto> GetAchatDetails(int id, CancellationToken ct)
    {
        var a = await _db.Achats
            .Include(a => a.Fournisseur).Include(a => a.Utilisateur)
            .Include(a => a.Lignes).ThenInclude(l => l.Produit)
            .Include(a => a.PaiementsAchat)
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
            .Include(a => a.Lignes).ThenInclude(l => l.Produit)
            .Include(a => a.PaiementsAchat)
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
            .Include(a => a.PaiementsAchat)
            .FirstOrDefaultAsync(a => a.Id == q.Id, ct)
            ?? throw new NotFoundException("Achat", q.Id);
        return AchatMapper.ToDto(a);
    }
}

// ============ PAIEMENTS ACHAT DTO + QUERY ============
public class PaiementAchatDto
{
    public int Id { get; set; }
    public int AchatId { get; set; }
    public string AchatReference { get; set; } = string.Empty;
    public int FournisseurId { get; set; }
    public string NomFournisseur { get; set; } = string.Empty;
    public string? IconeFournisseur { get; set; }
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public string MethodeLibelle => Methode.ToString();
    public StatutPaiement Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public DateTime DatePaiement { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public record GetPaiementsAchatQuery(
    int Page = 1, int PageSize = 200,
    string? Search = null,
    StatutPaiement? Statut = null,
    MethodePaiement? Methode = null
) : IRequest<PagedList<PaiementAchatDto>>;

public class GetPaiementsAchatHandler : IRequestHandler<GetPaiementsAchatQuery, PagedList<PaiementAchatDto>>
{
    private readonly IAppDbContext _db;
    public GetPaiementsAchatHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<PaiementAchatDto>> Handle(GetPaiementsAchatQuery q, CancellationToken ct)
    {
        var query = _db.PaiementsAchat
            .Include(p => p.Achat).ThenInclude(a => a.Fournisseur)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(p =>
                p.Achat.Reference.ToLower().Contains(s) ||
                p.Achat.Fournisseur.Nom.ToLower().Contains(s));
        }

        if (q.Statut.HasValue) query = query.Where(p => p.Statut == q.Statut);
        if (q.Methode.HasValue) query = query.Where(p => p.Methode == q.Methode);

        query = query.OrderByDescending(p => p.DatePaiement);

        var paged = await PagedList<PaiementAchat>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<PaiementAchatDto>
        {
            Items = paged.Items.Select(p => new PaiementAchatDto
            {
                Id = p.Id,
                AchatId = p.AchatId,
                AchatReference = p.Achat?.Reference ?? "",
                FournisseurId = p.Achat?.FournisseurId ?? 0,
                NomFournisseur = p.Achat?.Fournisseur?.Nom ?? "",
                IconeFournisseur = p.Achat?.Fournisseur?.Icone,
                Montant = p.Montant,
                Methode = p.Methode,
                Statut = p.Statut,
                DatePaiement = p.DatePaiement,
                Reference = p.Reference,
                Notes = p.Notes
            }).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page, PageSize = paged.PageSize
        };
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
        DateAchat = a.DateAchat,
        MontantTotal = a.MontantTotal,
        MontantPaye = a.MontantPaye,
        Reste = a.Reste,
        ProgressionPaiement = a.ProgressionPaiement,
        Statut = a.Statut,
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
