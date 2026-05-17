using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Devis;

// ============ DTOs ============
public class DevisDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string? ClientInitiales { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public DateTime DateDevis { get; set; }
    public DateTime? DateValidite { get; set; }
    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; }
    public StatutDevis Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public string? Notes { get; set; }
    public int? VenteId { get; set; }
    public string? VenteReference { get; set; }
    public int NombreArticles { get; set; }
    public bool EstExpire { get; set; }
    public List<LigneDevisDto> Lignes { get; set; } = new();
}

public class LigneDevisDto
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string ReferenceProduit { get; set; } = string.Empty;
    public int Quantite { get; set; }
    public decimal Remise { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Tva { get; set; }
    public decimal Total { get; set; }
}

public class CreateDevisDto
{
    public int ClientId { get; set; }
    public DateTime? DateDevis { get; set; }
    public DateTime? DateValidite { get; set; }
    public string? Notes { get; set; }
    public List<CreateLigneDevisDto> Lignes { get; set; } = new();
}

public class UpdateDevisDto
{
    public int ClientId { get; set; }
    public DateTime? DateValidite { get; set; }
    public string? Notes { get; set; }
    public List<CreateLigneDevisDto> Lignes { get; set; } = new();
}

public class CreateLigneDevisDto
{
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; } = 0;
    public decimal Tva { get; set; } = 20;
}

public class UpdateDevisStatutDto
{
    public StatutDevis Statut { get; set; }
}

public class ConversionResultDto
{
    public int VenteId { get; set; }
    public string VenteReference { get; set; } = string.Empty;
    public string DevisReference { get; set; } = string.Empty;
}

// ============ COMMANDS ============
public record CreateDevisCommand(CreateDevisDto Dto) : IRequest<DevisDto>;
public record UpdateDevisCommand(int Id, UpdateDevisDto Dto) : IRequest<DevisDto>;
public record UpdateDevisStatutCommand(int Id, StatutDevis Statut) : IRequest<DevisDto>;
public record ConvertirDevisEnVenteCommand(int Id) : IRequest<ConversionResultDto>;
public record DeleteDevisCommand(int Id) : IRequest;

// ============ VALIDATORS ============
public class CreateDevisValidator : AbstractValidator<CreateDevisCommand>
{
    public CreateDevisValidator()
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

// ============ HANDLERS ============
public class CreateDevisHandler : IRequestHandler<CreateDevisCommand, DevisDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public CreateDevisHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<DevisDto> Handle(CreateDevisCommand req, CancellationToken ct)
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

        var reference = await _refGen.GenerateDevisReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var devis = new Domain.Entities.Devis
        {
            Reference = reference,
            ClientId = dto.ClientId,
            UtilisateurId = userId,
            DateDevis = dto.DateDevis ?? now,
            DateValidite = dto.DateValidite,
            Notes = dto.Notes?.Trim(),
            Statut = StatutDevis.Brouillon,
            Lignes = dto.Lignes.Select(l => new LigneDevis
            {
                ProduitId = l.ProduitId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                Remise = l.Remise,
                Tva = l.Tva
            }).ToList()
        };

        devis.MontantTotalHT = devis.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100));
        devis.MontantTVA = devis.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100) * (l.Tva / 100));
        devis.MontantTotal = devis.MontantTotalHT + devis.MontantTVA;

        _db.Devis.Add(devis);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "devis",
            $"Devis créé : {devis.Reference} pour {client.NomClient} ({devis.MontantTotal:N2} MAD)",
            devis.Id, devis.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Nouveau devis — {devis.Reference}",
            message: $"Devis de {devis.MontantTotal:N2} MAD créé pour {client.NomClient}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Vente,
            entiteId: devis.Id, entiteReference: devis.Reference,
            lienUrl: "/devis", ct: ct);

        return await LoadDtoHelper.LoadDto(_db, devis.Id, ct);
    }
}

public class UpdateDevisHandler : IRequestHandler<UpdateDevisCommand, DevisDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public UpdateDevisHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<DevisDto> Handle(UpdateDevisCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var devis = await _db.Devis.Include(d => d.Lignes)
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new NotFoundException("Devis", req.Id);

        if (devis.Statut != StatutDevis.Brouillon)
            throw new BusinessException("Seul un devis en brouillon peut être modifié");

        var dto = req.Dto;

        if (!await _db.Clients.AnyAsync(c => c.Id == dto.ClientId, ct))
            throw new NotFoundException("Client", dto.ClientId);

        var produitIds = dto.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits.Where(p => produitIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var l in dto.Lignes)
            if (!produits.Any(p => p.Id == l.ProduitId))
                throw new NotFoundException("Produit", l.ProduitId);

        // Replace lines
        foreach (var ligne in devis.Lignes.ToList())
            _db.LignesDevis.Remove(ligne);

        devis.ClientId = dto.ClientId;
        devis.DateValidite = dto.DateValidite;
        devis.Notes = dto.Notes?.Trim();
        devis.Lignes = dto.Lignes.Select(l => new LigneDevis
        {
            ProduitId = l.ProduitId,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise,
            Tva = l.Tva
        }).ToList();

        devis.MontantTotalHT = devis.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100));
        devis.MontantTVA = devis.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100) * (l.Tva / 100));
        devis.MontantTotal = devis.MontantTotalHT + devis.MontantTVA;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "devis",
            $"Devis modifié : {devis.Reference}",
            devis.Id, devis.Reference, ct: ct);

        return await LoadDtoHelper.LoadDto(_db, devis.Id, ct);
    }
}

public class UpdateDevisStatutHandler : IRequestHandler<UpdateDevisStatutCommand, DevisDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public UpdateDevisStatutHandler(IAppDbContext db, ICurrentUserService current,
        IAuditLogger audit, INotificationService notif)
    {
        _db = db; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<DevisDto> Handle(UpdateDevisStatutCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var devis = await _db.Devis.Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new NotFoundException("Devis", req.Id);

        var allowedTransitions = new Dictionary<StatutDevis, List<StatutDevis>>
        {
            [StatutDevis.Brouillon] = new() { StatutDevis.Envoye },
            [StatutDevis.Envoye]    = new() { StatutDevis.Accepte, StatutDevis.Refuse, StatutDevis.Expire },
        };

        if (!allowedTransitions.TryGetValue(devis.Statut, out var allowed) || !allowed.Contains(req.Statut))
            throw new BusinessException($"Transition {devis.Statut} → {req.Statut} non autorisée");

        var ancienStatut = devis.Statut;
        devis.Statut = req.Statut;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "devis",
            $"Statut devis {devis.Reference} : {ancienStatut} → {req.Statut}",
            devis.Id, devis.Reference, ct: ct);

        if (req.Statut == StatutDevis.Accepte)
            await _notif.CreateAsync(
                titre: $"Devis accepté — {devis.Reference}",
                message: $"Le devis {devis.Reference} de {devis.MontantTotal:N2} MAD a été accepté par {devis.Client?.NomClient}.",
                type: TypeNotification.Success,
                categorie: CategorieNotification.Vente,
                entiteId: devis.Id, entiteReference: devis.Reference,
                lienUrl: "/devis", ct: ct);
        else if (req.Statut == StatutDevis.Refuse)
            await _notif.CreateAsync(
                titre: $"Devis refusé — {devis.Reference}",
                message: $"Le devis {devis.Reference} de {devis.MontantTotal:N2} MAD a été refusé.",
                type: TypeNotification.Warning,
                categorie: CategorieNotification.Vente,
                entiteId: devis.Id, entiteReference: devis.Reference,
                lienUrl: "/devis", ct: ct);

        return await LoadDtoHelper.LoadDto(_db, devis.Id, ct);
    }
}

public class ConvertirDevisEnVenteHandler : IRequestHandler<ConvertirDevisEnVenteCommand, ConversionResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public ConvertirDevisEnVenteHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<ConversionResultDto> Handle(ConvertirDevisEnVenteCommand req, CancellationToken ct)
    {
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var devis = await _db.Devis
            .Include(d => d.Lignes)
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new NotFoundException("Devis", req.Id);

        if (devis.Statut != StatutDevis.Accepte)
            throw new BusinessException("Seul un devis accepté peut être converti en vente");

        // Check stock
        var produitIds = devis.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits.Where(p => produitIds.Contains(p.Id)).ToListAsync(ct);

        foreach (var ligne in devis.Lignes)
        {
            var produit = produits.FirstOrDefault(p => p.Id == ligne.ProduitId)
                ?? throw new NotFoundException("Produit", ligne.ProduitId);
            if (produit.QuantiteStock < ligne.Quantite)
                throw new BusinessException(
                    $"Stock insuffisant pour '{produit.Nom}'. Disponible : {produit.QuantiteStock}, requis : {ligne.Quantite}");
        }

        // Create vente
        var venteRef = await _refGen.GenerateSaleReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var vente = new Vente
        {
            Reference = venteRef,
            ClientId = devis.ClientId,
            UtilisateurId = userId,
            DateVente = now,
            DateEcheance = now.AddDays(30),
            Statut = StatutVente.EnAttente,
            Lignes = devis.Lignes.Select(l => new LigneVente
            {
                ProduitId = l.ProduitId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                Remise = l.Remise,
                TVA = l.Tva
            }).ToList()
        };

        vente.MontantTotalHT = vente.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100));
        vente.MontantTVA = vente.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (1 - l.Remise / 100) * (l.TVA / 100));
        vente.MontantTotal = vente.MontantTotalHT + vente.MontantTVA;

        _db.Ventes.Add(vente);
        await _db.SaveChangesAsync(ct);

        // Reduce stock
        foreach (var ligne in vente.Lignes)
        {
            var produit = produits.First(p => p.Id == ligne.ProduitId);
            var stockAvant = produit.QuantiteStock;
            produit.QuantiteStock -= ligne.Quantite;

            _db.MouvementsStock.Add(new MouvementStock
            {
                ProduitId = produit.Id,
                Type = TypeMouvementStock.Sortie,
                Quantite = ligne.Quantite,
                StockAvant = stockAvant,
                StockApres = produit.QuantiteStock,
                Source = SourceMouvementStock.Vente,
                ReferenceId = vente.Id,
                ReferenceText = vente.Reference,
                UtilisateurId = userId,
                DateMouvement = now
            });
        }

        // Create facture
        var factureRef = await _refGen.GenerateInvoiceReferenceAsync(ct);
        _db.Factures.Add(new Facture
        {
            NumeroFacture = factureRef,
            VenteId = vente.Id,
            DateEmission = now,
            DateEcheance = now.AddDays(30),
            Statut = StatutFacture.EnAttente
        });

        // Mark devis converted
        devis.Statut = StatutDevis.Converti;
        devis.VenteId = vente.Id;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "ventes",
            $"Vente {vente.Reference} créée depuis devis {devis.Reference}",
            vente.Id, vente.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Devis converti — {devis.Reference}",
            message: $"Le devis {devis.Reference} a été converti en vente {vente.Reference} ({vente.MontantTotal:N2} MAD).",
            type: TypeNotification.Success,
            categorie: CategorieNotification.Vente,
            entiteId: vente.Id, entiteReference: vente.Reference,
            lienUrl: "/ventes", ct: ct);

        return new ConversionResultDto
        {
            VenteId = vente.Id,
            VenteReference = vente.Reference,
            DevisReference = devis.Reference
        };
    }
}

public class DeleteDevisHandler : IRequestHandler<DeleteDevisCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public DeleteDevisHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task Handle(DeleteDevisCommand req, CancellationToken ct)
    {
        if (_current.UserId == null) throw new UnauthorizedException();

        var devis = await _db.Devis.FirstOrDefaultAsync(d => d.Id == req.Id, ct)
            ?? throw new NotFoundException("Devis", req.Id);

        if (devis.Statut != StatutDevis.Brouillon)
            throw new BusinessException("Seul un devis en brouillon peut être supprimé");

        _db.Devis.Remove(devis);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Delete, "devis",
            $"Devis supprimé : {devis.Reference}",
            devis.Id, devis.Reference, ct: ct);
    }
}

// ============ QUERIES ============
public record GetDevisQuery(
    int Page = 1, int PageSize = 200,
    string? Search = null,
    StatutDevis? Statut = null,
    int? ClientId = null,
    DateTime? DateDebut = null,
    DateTime? DateFin = null
) : IRequest<PagedList<DevisDto>>;

public class GetDevisHandler : IRequestHandler<GetDevisQuery, PagedList<DevisDto>>
{
    private readonly IAppDbContext _db;
    public GetDevisHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<DevisDto>> Handle(GetDevisQuery q, CancellationToken ct)
    {
        var query = _db.Devis
            .Include(d => d.Client)
            .Include(d => d.Utilisateur)
            .Include(d => d.Lignes)
            .Include(d => d.Vente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(d =>
                d.Reference.ToLower().Contains(s) ||
                d.Client.NomClient.ToLower().Contains(s));
        }

        if (q.Statut.HasValue) query = query.Where(d => d.Statut == q.Statut);
        if (q.ClientId.HasValue) query = query.Where(d => d.ClientId == q.ClientId);
        if (q.DateDebut.HasValue) query = query.Where(d => d.DateDevis >= q.DateDebut.Value.Date);
        if (q.DateFin.HasValue) query = query.Where(d => d.DateDevis < q.DateFin.Value.Date.AddDays(1));

        query = query.OrderByDescending(d => d.DateDevis);

        var paged = await PagedList<Domain.Entities.Devis>.CreateAsync(query, q.Page, q.PageSize, ct);
        var now = DateTime.UtcNow;
        return new PagedList<DevisDto>
        {
            Items = paged.Items.Select(d => DevisMapper.ToDto(d, now)).ToList(),
            TotalCount = paged.TotalCount, Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public record GetDevisByIdQuery(int Id) : IRequest<DevisDto>;
public record GenerateDevisPdfQuery(int Id, EntrepriseInfoDto? Info = null) : IRequest<byte[]>;
public record EnvoyerDevisEmailCommand(int Id, string ToEmail, string? Message = null, EntrepriseInfoDto? Info = null) : IRequest;

public class GetDevisByIdHandler : IRequestHandler<GetDevisByIdQuery, DevisDto>
{
    private readonly IAppDbContext _db;
    public GetDevisByIdHandler(IAppDbContext db) => _db = db;

    public async Task<DevisDto> Handle(GetDevisByIdQuery q, CancellationToken ct)
    {
        var d = await _db.Devis
            .Include(x => x.Client)
            .Include(x => x.Utilisateur)
            .Include(x => x.Lignes).ThenInclude(l => l.Produit)
            .Include(x => x.Vente)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct)
            ?? throw new NotFoundException("Devis", q.Id);
        return DevisMapper.ToDto(d, DateTime.UtcNow);
    }
}

public class GenerateDevisPdfHandler : IRequestHandler<GenerateDevisPdfQuery, byte[]>
{
    private readonly IPdfService _pdf;
    public GenerateDevisPdfHandler(IPdfService pdf) => _pdf = pdf;

    public Task<byte[]> Handle(GenerateDevisPdfQuery q, CancellationToken ct)
        => _pdf.GenerateDevisPdfAsync(q.Id, q.Info, ct);
}

public class EnvoyerDevisEmailHandler : IRequestHandler<EnvoyerDevisEmailCommand>
{
    private readonly IEmailService _email;
    public EnvoyerDevisEmailHandler(IEmailService email) => _email = email;

    public Task Handle(EnvoyerDevisEmailCommand cmd, CancellationToken ct)
        => _email.SendDevisAsync(cmd.Id, cmd.ToEmail, cmd.Message, cmd.Info, ct);
}

// ============ MAPPER ============
public static class DevisMapper
{
    public static DevisDto ToDto(Domain.Entities.Devis d, DateTime now) => new()
    {
        Id = d.Id,
        Reference = d.Reference,
        ClientId = d.ClientId,
        NomClient = d.Client?.NomClient ?? "",
        ClientInitiales = d.Client?.Initiales,
        UtilisateurId = d.UtilisateurId,
        NomUtilisateur = d.Utilisateur?.NomComplet ?? "",
        DateDevis = d.DateDevis,
        DateValidite = d.DateValidite,
        MontantTotalHT = d.MontantTotalHT,
        MontantTVA = d.MontantTVA,
        MontantTotal = d.MontantTotal,
        Statut = d.Statut,
        Notes = d.Notes,
        VenteId = d.VenteId,
        VenteReference = d.Vente?.Reference,
        NombreArticles = d.Lignes?.Sum(l => l.Quantite) ?? 0,
        EstExpire = d.DateValidite.HasValue && d.DateValidite < now
                    && (d.Statut == StatutDevis.Brouillon || d.Statut == StatutDevis.Envoye),
        Lignes = d.Lignes?.Select(l => new LigneDevisDto
        {
            Id = l.Id,
            ProduitId = l.ProduitId,
            NomProduit = l.Produit?.Nom ?? "",
            ReferenceProduit = l.Produit?.Reference ?? "",
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise,
            Tva = l.Tva,
            Total = l.Total
        }).ToList() ?? new()
    };
}

// ============ HELPERS ============
internal static class LoadDtoHelper
{
    internal static async Task<DevisDto> LoadDto(IAppDbContext db, int id, CancellationToken ct)
    {
        var d = await db.Devis
            .Include(x => x.Client)
            .Include(x => x.Utilisateur)
            .Include(x => x.Lignes).ThenInclude(l => l.Produit)
            .Include(x => x.Vente)
            .FirstAsync(x => x.Id == id, ct);
        return DevisMapper.ToDto(d, DateTime.UtcNow);
    }
}

public record DevisStatsDto(int Total, int Acceptes, int Convertis, decimal MontantPotentiel, int TauxAcceptation);
public record GetDevisStatsQuery() : IRequest<DevisStatsDto>;

public class GetDevisStatsHandler(IAppDbContext db) : IRequestHandler<GetDevisStatsQuery, DevisStatsDto>
{
    public async Task<DevisStatsDto> Handle(GetDevisStatsQuery _, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var total    = await db.Devis.CountAsync(d => d.DateDevis >= startOfMonth, ct);
        var acceptes = await db.Devis.CountAsync(d => d.DateDevis >= startOfMonth && d.Statut == StatutDevis.Accepte, ct);
        var convertis = await db.Devis.CountAsync(d => d.Statut == StatutDevis.Converti, ct);
        var montantPotentiel = await db.Devis
            .Where(d => d.Statut == StatutDevis.Envoye || d.Statut == StatutDevis.Brouillon)
            .SumAsync(d => (decimal?)d.MontantTotal, ct) ?? 0;
        var tauxAcceptation = total > 0 ? (int)Math.Round((double)acceptes / total * 100) : 0;

        return new DevisStatsDto(total, acceptes, convertis, montantPotentiel, tauxAcceptation);
    }
}
