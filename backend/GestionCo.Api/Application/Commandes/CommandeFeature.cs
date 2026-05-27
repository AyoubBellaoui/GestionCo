using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Commandes;

// ============ DTOs ============
public class CommandeDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string? ClientInitiales { get; set; }
    public int? DevisId { get; set; }
    public string? DevisReference { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public DateTime DateCommande { get; set; }
    public DateTime? DateLivraison { get; set; }
    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; }
    public StatutCommande Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public EtatLivraison EtatLivraison { get; set; }
    public string EtatLivraisonLibelle => EtatLivraison.ToString();
    public string? Notes { get; set; }
    public int? VenteId { get; set; }
    public string? VenteReference { get; set; }
    public int NombreArticles { get; set; }
    public List<LigneCommandeDto> Lignes { get; set; } = new();
}

public class LigneCommandeDto
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string ReferenceProduit { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; }
    public decimal Tva { get; set; }
    public decimal Total { get; set; }
}

public class CreateCommandeDto
{
    public int ClientId { get; set; }
    public DateTime? DateCommande { get; set; }
    public DateTime? DateLivraison { get; set; }
    public string? Notes { get; set; }
    public List<CreateLigneCommandeDto> Lignes { get; set; } = new();
}

public class UpdateCommandeDto
{
    public int ClientId { get; set; }
    public DateTime? DateLivraison { get; set; }
    public string? Notes { get; set; }
    public List<CreateLigneCommandeDto> Lignes { get; set; } = new();
}

public class CreateLigneCommandeDto
{
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; } = 0;
    public decimal Tva { get; set; } = 20;
}

public class UpdateCommandeStatutDto
{
    public StatutCommande Statut { get; set; }
}

public class UpdateCommandeEtatLivraisonDto
{
    public EtatLivraison EtatLivraison { get; set; }
}

public class ConversionCommandeResultDto
{
    public int CommandeId { get; set; }
    public string CommandeReference { get; set; } = string.Empty;
    public int? DevisId { get; set; }
    public string? DevisReference { get; set; }
}

// ============ COMMANDS ============
public record CreateCommandeCommand(CreateCommandeDto Dto) : IRequest<CommandeDto>;
public record UpdateCommandeCommand(int Id, UpdateCommandeDto Dto) : IRequest<CommandeDto>;
public record UpdateCommandeStatutCommand(int Id, StatutCommande Statut) : IRequest<CommandeDto>;
public record UpdateCommandeEtatLivraisonCommand(int Id, EtatLivraison EtatLivraison) : IRequest<CommandeDto>;
public record DeleteCommandeCommand(int Id) : IRequest;

// ============ QUERIES ============
public record GetCommandesQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    StatutCommande? Statut = null,
    int? ClientId = null,
    string? DateDebut = null,
    string? DateFin = null
) : IRequest<PagedList<CommandeDto>>;

public record GetCommandeByIdQuery(int Id) : IRequest<CommandeDto>;
public record GetCommandeStatsQuery() : IRequest<CommandeStatsDto>;

public class CommandeStatsDto
{
    public int Total { get; set; }
    public int EnAttente { get; set; }
    public int Confirmees { get; set; }
    public int Converties { get; set; }
    public decimal MontantTotal { get; set; }
}

// ============ VALIDATORS ============
public class CreateCommandeValidator : AbstractValidator<CreateCommandeCommand>
{
    public CreateCommandeValidator()
    {
        RuleFor(x => x.Dto.ClientId).GreaterThan(0).WithMessage("Client requis");
        RuleFor(x => x.Dto.Lignes).NotEmpty().WithMessage("Au moins une ligne requise");
        RuleForEach(x => x.Dto.Lignes).ChildRules(l =>
        {
            l.RuleFor(x => x.ProduitId).GreaterThan(0);
            l.RuleFor(x => x.Quantite).GreaterThan(0);
            l.RuleFor(x => x.PrixUnitaire).GreaterThanOrEqualTo(0);
        });
    }
}

// ============ LOAD HELPER ============
internal static class CommandeLoadHelper
{
    public static async Task<CommandeDto> LoadDto(IAppDbContext db, int id, CancellationToken ct)
    {
        var c = await db.Commandes
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Utilisateur)
            .Include(x => x.Devis)
            .Include(x => x.Vente)
            .Include(x => x.Lignes).ThenInclude(l => l.Produit)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Commande", id);

        return Map(c);
    }

    public static CommandeDto Map(Commande c) => new()
    {
        Id = c.Id,
        Reference = c.Reference,
        ClientId = c.ClientId,
        NomClient = c.Client?.NomClient ?? string.Empty,
        ClientInitiales = c.Client?.NomClient is { Length: > 0 } n
            ? string.Concat(n.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(w => w[0].ToString().ToUpper()))
            : null,
        DevisId = c.DevisId,
        DevisReference = c.Devis?.Reference,
        UtilisateurId = c.UtilisateurId,
        NomUtilisateur = c.Utilisateur != null ? $"{c.Utilisateur.Prenom} {c.Utilisateur.Nom}" : string.Empty,
        DateCommande = c.DateCommande,
        DateLivraison = c.DateLivraison,
        MontantTotalHT = c.MontantTotalHT,
        MontantTVA = c.MontantTVA,
        MontantTotal = c.MontantTotal,
        Statut = c.Statut,
        EtatLivraison = c.EtatLivraison,
        Notes = c.Notes,
        VenteId = c.VenteId,
        VenteReference = c.Vente?.Reference,
        NombreArticles = c.Lignes.Count,
        Lignes = c.Lignes.Select(l => new LigneCommandeDto
        {
            Id = l.Id,
            ProduitId = l.ProduitId,
            NomProduit = l.Produit?.Nom ?? string.Empty,
            ReferenceProduit = l.Produit?.Reference ?? string.Empty,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise,
            Tva = l.Tva,
            Total = l.Total
        }).ToList()
    };
}

// ============ HANDLERS ============
public class CreateCommandeHandler : IRequestHandler<CreateCommandeCommand, CommandeDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public CreateCommandeHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<CommandeDto> Handle(CreateCommandeCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == dto.ClientId, ct)
            ?? throw new NotFoundException("Client", dto.ClientId);

        var produitIds = dto.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits.Where(p => produitIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var l in dto.Lignes)
            if (!produits.Any(p => p.Id == l.ProduitId))
                throw new NotFoundException("Produit", l.ProduitId);

        var reference = await _refGen.GenerateCommandeReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var commande = new Commande
        {
            Reference = reference,
            ClientId = dto.ClientId,
            UtilisateurId = userId,
            DateCommande = dto.DateCommande ?? now,
            DateLivraison = dto.DateLivraison,
            Notes = dto.Notes?.Trim(),
            Statut = StatutCommande.Brouillon,
            EtatLivraison = EtatLivraison.NonCommence,
            Lignes = dto.Lignes.Select(l => new LigneCommande
            {
                ProduitId = l.ProduitId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                Remise = l.Remise,
                Tva = l.Tva
            }).ToList()
        };

        commande.MontantTotalHT = commande.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100));
        commande.MontantTVA = commande.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100) * (l.Tva / 100));
        commande.MontantTotal = commande.MontantTotalHT + commande.MontantTVA;

        _db.Commandes.Add(commande);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "commande",
            $"Commande créée : {commande.Reference} pour {client.NomClient} ({commande.MontantTotal:N2} MAD)",
            commande.Id, commande.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Nouvelle commande — {commande.Reference}",
            message: $"Commande de {commande.MontantTotal:N2} MAD créée pour {client.NomClient}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Vente,
            entiteId: commande.Id, entiteReference: commande.Reference,
            lienUrl: "/commandes", ct: ct);

        return await CommandeLoadHelper.LoadDto(_db, commande.Id, ct);
    }
}

public class UpdateCommandeHandler : IRequestHandler<UpdateCommandeCommand, CommandeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public UpdateCommandeHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<CommandeDto> Handle(UpdateCommandeCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var commande = await _db.Commandes.Include(c => c.Lignes)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Commande", req.Id);

        if (commande.Statut == StatutCommande.Convertie || commande.Statut == StatutCommande.Annulee)
            throw new BusinessException("Une commande convertie ou annulée ne peut plus être modifiée");

        var dto = req.Dto;
        if (!await _db.Clients.AnyAsync(c => c.Id == dto.ClientId, ct))
            throw new NotFoundException("Client", dto.ClientId);

        var produitIds = dto.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits.Where(p => produitIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var l in dto.Lignes)
            if (!produits.Any(p => p.Id == l.ProduitId))
                throw new NotFoundException("Produit", l.ProduitId);

        foreach (var ligne in commande.Lignes.ToList())
            _db.LignesCommande.Remove(ligne);

        commande.ClientId = dto.ClientId;
        commande.DateLivraison = dto.DateLivraison;
        commande.Notes = dto.Notes?.Trim();
        commande.Lignes = dto.Lignes.Select(l => new LigneCommande
        {
            ProduitId = l.ProduitId,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise,
            Tva = l.Tva
        }).ToList();

        commande.MontantTotalHT = commande.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100));
        commande.MontantTVA = commande.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100) * (l.Tva / 100));
        commande.MontantTotal = commande.MontantTotalHT + commande.MontantTVA;

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(ActionLog.Update, "commande",
            $"Commande modifiée : {commande.Reference} — {commande.Lignes.Count} ligne(s) · TTC: {commande.MontantTotal:N2} MAD",
            commande.Id, commande.Reference, ct: ct);

        return await CommandeLoadHelper.LoadDto(_db, commande.Id, ct);
    }
}

public class UpdateCommandeStatutHandler : IRequestHandler<UpdateCommandeStatutCommand, CommandeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public UpdateCommandeStatutHandler(IAppDbContext db, ICurrentUserService current,
        IAuditLogger audit, INotificationService notif)
    {
        _db = db; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<CommandeDto> Handle(UpdateCommandeStatutCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var commande = await _db.Commandes.Include(c => c.Client)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Commande", req.Id);

        var allowedTransitions = new Dictionary<StatutCommande, List<StatutCommande>>
        {
            [StatutCommande.Brouillon] = new() { StatutCommande.Confirmee, StatutCommande.Annulee },
            [StatutCommande.Confirmee] = new() { StatutCommande.Annulee },
        };

        if (!allowedTransitions.TryGetValue(commande.Statut, out var allowed) || !allowed.Contains(req.Statut))
            throw new BusinessException($"Transition {commande.Statut} → {req.Statut} non autorisée");

        var ancienStatut = commande.Statut;
        commande.Statut = req.Statut;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "commande",
            $"Statut commande {commande.Reference} : {ancienStatut} → {req.Statut}",
            commande.Id, commande.Reference, ct: ct);

        return await CommandeLoadHelper.LoadDto(_db, commande.Id, ct);
    }
}

public class UpdateCommandeEtatLivraisonHandler : IRequestHandler<UpdateCommandeEtatLivraisonCommand, CommandeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public UpdateCommandeEtatLivraisonHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<CommandeDto> Handle(UpdateCommandeEtatLivraisonCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var commande = await _db.Commandes
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Commande", req.Id);

        if (commande.Statut != StatutCommande.Confirmee)
            throw new BusinessException("L'état de livraison ne peut être modifié que pour une commande confirmée");

        var allowedTransitions = new Dictionary<EtatLivraison, List<EtatLivraison>>
        {
            [EtatLivraison.NonCommence]   = new() { EtatLivraison.EnPreparation },
            [EtatLivraison.EnPreparation] = new() { EtatLivraison.EnLivraison },
            [EtatLivraison.EnLivraison]   = new() { EtatLivraison.Livre },
        };

        if (!allowedTransitions.TryGetValue(commande.EtatLivraison, out var allowed) || !allowed.Contains(req.EtatLivraison))
            throw new BusinessException($"Transition {commande.EtatLivraison} → {req.EtatLivraison} non autorisée");

        var ancien = commande.EtatLivraison;
        commande.EtatLivraison = req.EtatLivraison;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "commande",
            $"État livraison {commande.Reference} : {ancien} → {req.EtatLivraison}",
            commande.Id, commande.Reference, ct: ct);

        return await CommandeLoadHelper.LoadDto(_db, commande.Id, ct);
    }
}

public class DeleteCommandeHandler : IRequestHandler<DeleteCommandeCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public DeleteCommandeHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task Handle(DeleteCommandeCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var commande = await _db.Commandes.Include(c => c.Lignes)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Commande", req.Id);

        if (commande.Statut == StatutCommande.Convertie)
            throw new BusinessException("Une commande convertie en vente ne peut pas être supprimée");

        _db.Commandes.Remove(commande);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Delete, "commande",
            $"Commande supprimée : {commande.Reference}",
            commande.Id, commande.Reference, ct: ct);
    }
}

public class GetCommandesHandler : IRequestHandler<GetCommandesQuery, PagedList<CommandeDto>>
{
    private readonly IAppDbContext _db;

    public GetCommandesHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<CommandeDto>> Handle(GetCommandesQuery req, CancellationToken ct)
    {
        var q = _db.Commandes
            .AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.Utilisateur)
            .Include(c => c.Devis)
            .Include(c => c.Vente)
            .Include(c => c.Lignes).ThenInclude(l => l.Produit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Search))
        {
            var s = req.Search.ToLower();
            q = q.Where(c => c.Reference.ToLower().Contains(s) || c.Client.NomClient.ToLower().Contains(s));
        }
        if (req.Statut.HasValue)
            q = q.Where(c => c.Statut == req.Statut);
        if (req.ClientId.HasValue)
            q = q.Where(c => c.ClientId == req.ClientId);
        if (!string.IsNullOrEmpty(req.DateDebut) && DateTime.TryParse(req.DateDebut, out var dd))
            q = q.Where(c => c.DateCommande >= dd);
        if (!string.IsNullOrEmpty(req.DateFin) && DateTime.TryParse(req.DateFin, out var df))
            q = q.Where(c => c.DateCommande <= df.AddDays(1));

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(c => c.DateCommande)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync(ct);

        return new PagedList<CommandeDto>
        {
            Items = items.Select(CommandeLoadHelper.Map).ToList(),
            TotalCount = total,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
}

public class GetCommandeByIdHandler : IRequestHandler<GetCommandeByIdQuery, CommandeDto>
{
    private readonly IAppDbContext _db;
    public GetCommandeByIdHandler(IAppDbContext db) => _db = db;

    public async Task<CommandeDto> Handle(GetCommandeByIdQuery req, CancellationToken ct)
        => await CommandeLoadHelper.LoadDto(_db, req.Id, ct);
}

public class GetCommandeStatsHandler : IRequestHandler<GetCommandeStatsQuery, CommandeStatsDto>
{
    private readonly IAppDbContext _db;
    public GetCommandeStatsHandler(IAppDbContext db) => _db = db;

    public async Task<CommandeStatsDto> Handle(GetCommandeStatsQuery req, CancellationToken ct)
    {
        var all = await _db.Commandes.AsNoTracking().ToListAsync(ct);
        return new CommandeStatsDto
        {
            Total = all.Count,
            EnAttente = all.Count(c => c.Statut == StatutCommande.Brouillon),
            Confirmees = all.Count(c => c.Statut == StatutCommande.Confirmee),
            Converties = all.Count(c => c.Statut == StatutCommande.Convertie),
            MontantTotal = all.Where(c => c.Statut != StatutCommande.Annulee).Sum(c => c.MontantTotal)
        };
    }
}
