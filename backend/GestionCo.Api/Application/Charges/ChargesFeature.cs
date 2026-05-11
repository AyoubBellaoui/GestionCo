using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Charges;

// ============ DTOs ============
public class ChargeDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Montant { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal Reste { get; set; }
    public int ProgressionPaiement { get; set; }
    public string? Justificatif { get; set; }
    public StatutCharge Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public DateTime DateCharge { get; set; }
    public int CategorieChargeId { get; set; }
    public string NomCategorie { get; set; } = string.Empty;
    public string? IconeCategorie { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public int? FournisseurId { get; set; }
    public string? NomFournisseur { get; set; }
    public string? IconeFournisseur { get; set; }
    public List<PaiementChargeDto> Paiements { get; set; } = new();
}

public class PaiementChargeDto
{
    public int Id { get; set; }
    public int ChargeId { get; set; }
    public string ChargeReference { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public string MethodeLibelle => Methode.ToString();
    public StatutPaiement Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public DateTime DatePaiement { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class CategorieChargeDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public int NombreCharges { get; set; }
}

public class CreateChargeDto
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Montant { get; set; }
    public int CategorieChargeId { get; set; }
    public DateTime? DateCharge { get; set; }
    public int? FournisseurId { get; set; }
    public string? Justificatif { get; set; }
    public decimal? PaiementInitial { get; set; }
    public MethodePaiement? MethodePaiementInitial { get; set; }
}

public class AddPaiementChargeDto
{
    public int ChargeId { get; set; }
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; } = MethodePaiement.Espece;
    public string? Notes { get; set; }
}

public class CreateCategorieChargeDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; }
}

// ============ COMMANDS ============
public record CreateChargeCommand(CreateChargeDto Dto) : IRequest<ChargeDto>;

public class CreateChargeValidator : AbstractValidator<CreateChargeCommand>
{
    public CreateChargeValidator()
    {
        RuleFor(x => x.Dto.Titre).NotEmpty().WithMessage("Le titre est requis").MaximumLength(200);
        RuleFor(x => x.Dto.Montant).GreaterThan(0).WithMessage("Le montant doit être supérieur à 0");
        RuleFor(x => x.Dto.CategorieChargeId).GreaterThan(0).WithMessage("La catégorie est requise");
    }
}

public record AddPaiementChargeCommand(AddPaiementChargeDto Dto) : IRequest<ChargeDto>;

public record CreateCategorieChargeCommand(CreateCategorieChargeDto Dto) : IRequest<CategorieChargeDto>;

public record DeleteCategorieChargeCommand(int Id) : IRequest;

// ============ HANDLERS ============
public class CreateChargeHandler : IRequestHandler<CreateChargeCommand, ChargeDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public CreateChargeHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<ChargeDto> Handle(CreateChargeCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        var categorie = await _db.CategoriesCharge.FirstOrDefaultAsync(c => c.Id == dto.CategorieChargeId, ct)
            ?? throw new NotFoundException("CategorieCharge", dto.CategorieChargeId);

        if (dto.FournisseurId.HasValue)
        {
            var exists = await _db.Fournisseurs.AnyAsync(f => f.Id == dto.FournisseurId.Value, ct);
            if (!exists) throw new NotFoundException("Fournisseur", dto.FournisseurId.Value);
        }

        var reference = await _refGen.GenerateChargeReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var charge = new Charge
        {
            Reference = reference,
            Titre = dto.Titre.Trim(),
            Description = dto.Description?.Trim(),
            Montant = dto.Montant,
            CategorieChargeId = dto.CategorieChargeId,
            DateCharge = dto.DateCharge ?? now,
            UtilisateurId = userId,
            FournisseurId = dto.FournisseurId,
            Justificatif = dto.Justificatif?.Trim()
        };

        var paiementInitial = dto.PaiementInitial ?? 0;
        if (paiementInitial > 0)
        {
            paiementInitial = Math.Min(paiementInitial, charge.Montant);
            charge.MontantPaye = paiementInitial;
            charge.Statut = paiementInitial >= charge.Montant ? StatutCharge.Paye : StatutCharge.Partiel;
            charge.Paiements.Add(new PaiementCharge
            {
                Montant = paiementInitial,
                Methode = dto.MethodePaiementInitial ?? MethodePaiement.Espece,
                Statut = StatutPaiement.Confirme,
                DatePaiement = now
            });
        }

        _db.Charges.Add(charge);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "charges",
            $"Charge créée : {charge.Reference} — {charge.Titre} ({charge.Montant:N2} MAD)",
            charge.Id, charge.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Nouvelle charge — {charge.Reference}",
            message: $"Charge « {charge.Titre} » de {charge.Montant:N2} MAD enregistrée.",
            type: TypeNotification.Warning,
            categorie: CategorieNotification.Charge,
            entiteId: charge.Id, entiteReference: charge.Reference,
            lienUrl: "/charges", ct: ct);

        return await GetChargeDetails(charge.Id, ct);
    }

    private async Task<ChargeDto> GetChargeDetails(int id, CancellationToken ct)
    {
        var c = await _db.Charges
            .Include(x => x.CategorieCharge)
            .Include(x => x.Utilisateur)
            .Include(x => x.Fournisseur)
            .Include(x => x.Paiements)
            .FirstAsync(x => x.Id == id, ct);
        return ChargeMapper.ToDto(c);
    }
}

public class AddPaiementChargeHandler : IRequestHandler<AddPaiementChargeCommand, ChargeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;
    private readonly INotificationService _notif;

    public AddPaiementChargeHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit, INotificationService notif)
    {
        _db = db; _current = current; _audit = audit; _notif = notif;
    }

    public async Task<ChargeDto> Handle(AddPaiementChargeCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        if (_current.UserId == null) throw new UnauthorizedException();

        var charge = await _db.Charges
            .Include(c => c.Paiements)
            .FirstOrDefaultAsync(c => c.Id == dto.ChargeId, ct)
            ?? throw new NotFoundException("Charge", dto.ChargeId);

        var montant = Math.Min(dto.Montant, charge.Reste);
        if (montant <= 0) throw new BusinessException("Montant invalide ou charge déjà soldée");

        charge.Paiements.Add(new PaiementCharge
        {
            ChargeId = charge.Id,
            Montant = montant,
            Methode = dto.Methode,
            Statut = StatutPaiement.Confirme,
            DatePaiement = DateTime.UtcNow,
            Notes = dto.Notes
        });

        charge.MontantPaye += montant;
        charge.Statut = charge.MontantPaye >= charge.Montant ? StatutCharge.Paye : StatutCharge.Partiel;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "charges",
            $"Paiement de {montant:N2} MAD ajouté à {charge.Reference}",
            charge.Id, charge.Reference, ct: ct);

        await _notif.CreateAsync(
            titre: $"Paiement charge — {charge.Reference}",
            message: $"Paiement de {montant:N2} MAD effectué pour la charge {charge.Reference}.",
            type: TypeNotification.Info,
            categorie: CategorieNotification.Paiement,
            entiteId: charge.Id, entiteReference: charge.Reference,
            lienUrl: "/charges", ct: ct);

        return await GetChargeDetails(charge.Id, ct);
    }

    private async Task<ChargeDto> GetChargeDetails(int id, CancellationToken ct)
    {
        var c = await _db.Charges
            .Include(x => x.CategorieCharge)
            .Include(x => x.Utilisateur)
            .Include(x => x.Fournisseur)
            .Include(x => x.Paiements)
            .FirstAsync(x => x.Id == id, ct);
        return ChargeMapper.ToDto(c);
    }
}

public class CreateCategorieChargeHandler : IRequestHandler<CreateCategorieChargeCommand, CategorieChargeDto>
{
    private readonly IAppDbContext _db;
    public CreateCategorieChargeHandler(IAppDbContext db) => _db = db;

    public async Task<CategorieChargeDto> Handle(CreateCategorieChargeCommand req, CancellationToken ct)
    {
        if (await _db.CategoriesCharge.AnyAsync(c => c.Nom.ToLower() == req.Dto.Nom.ToLower().Trim(), ct))
            throw new BusinessException($"La catégorie « {req.Dto.Nom} » existe déjà");

        var cat = new CategorieCharge { Nom = req.Dto.Nom.Trim(), Icone = req.Dto.Icone };
        _db.CategoriesCharge.Add(cat);
        await _db.SaveChangesAsync(ct);

        return new CategorieChargeDto { Id = cat.Id, Nom = cat.Nom, Icone = cat.Icone };
    }
}

public class DeleteCategorieChargeHandler : IRequestHandler<DeleteCategorieChargeCommand>
{
    private readonly IAppDbContext _db;
    public DeleteCategorieChargeHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteCategorieChargeCommand req, CancellationToken ct)
    {
        var cat = await _db.CategoriesCharge.FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("CategorieCharge", req.Id);

        var hasCharges = await _db.Charges.AnyAsync(c => c.CategorieChargeId == req.Id, ct);
        if (hasCharges) throw new BusinessException("Impossible de supprimer une catégorie liée à des charges");

        _db.CategoriesCharge.Remove(cat);
        await _db.SaveChangesAsync(ct);
    }
}

// ============ QUERIES ============
public record GetChargesQuery(
    int Page = 1, int PageSize = 200,
    string? Search = null,
    int? CategorieId = null,
    int? FournisseurId = null,
    StatutCharge? Statut = null,
    DateTime? DateDebut = null,
    DateTime? DateFin = null
) : IRequest<PagedList<ChargeDto>>;

public class GetChargesHandler : IRequestHandler<GetChargesQuery, PagedList<ChargeDto>>
{
    private readonly IAppDbContext _db;
    public GetChargesHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<ChargeDto>> Handle(GetChargesQuery q, CancellationToken ct)
    {
        var query = _db.Charges
            .Include(c => c.CategorieCharge)
            .Include(c => c.Utilisateur)
            .Include(c => c.Fournisseur)
            .Include(c => c.Paiements)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(c =>
                c.Reference.ToLower().Contains(s) ||
                c.Titre.ToLower().Contains(s) ||
                (c.Description != null && c.Description.ToLower().Contains(s)));
        }

        if (q.CategorieId.HasValue) query = query.Where(c => c.CategorieChargeId == q.CategorieId);
        if (q.FournisseurId.HasValue) query = query.Where(c => c.FournisseurId == q.FournisseurId);
        if (q.Statut.HasValue) query = query.Where(c => c.Statut == q.Statut);
        if (q.DateDebut.HasValue) query = query.Where(c => c.DateCharge >= q.DateDebut);
        if (q.DateFin.HasValue) query = query.Where(c => c.DateCharge <= q.DateFin);

        query = query.OrderByDescending(c => c.DateCharge);

        var paged = await PagedList<Charge>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<ChargeDto>
        {
            Items = paged.Items.Select(ChargeMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount, Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public record GetChargeByIdQuery(int Id) : IRequest<ChargeDto>;

public class GetChargeByIdHandler : IRequestHandler<GetChargeByIdQuery, ChargeDto>
{
    private readonly IAppDbContext _db;
    public GetChargeByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ChargeDto> Handle(GetChargeByIdQuery q, CancellationToken ct)
    {
        var c = await _db.Charges
            .Include(x => x.CategorieCharge)
            .Include(x => x.Utilisateur)
            .Include(x => x.Fournisseur)
            .Include(x => x.Paiements)
            .FirstOrDefaultAsync(x => x.Id == q.Id, ct)
            ?? throw new NotFoundException("Charge", q.Id);
        return ChargeMapper.ToDto(c);
    }
}

public record GetCategoriesChargeQuery : IRequest<List<CategorieChargeDto>>;

public class GetCategoriesChargeHandler : IRequestHandler<GetCategoriesChargeQuery, List<CategorieChargeDto>>
{
    private readonly IAppDbContext _db;
    public GetCategoriesChargeHandler(IAppDbContext db) => _db = db;

    public async Task<List<CategorieChargeDto>> Handle(GetCategoriesChargeQuery q, CancellationToken ct)
    {
        var cats = await _db.CategoriesCharge
            .Include(c => c.Charges)
            .OrderBy(c => c.Nom)
            .ToListAsync(ct);

        return cats.Select(c => new CategorieChargeDto
        {
            Id = c.Id,
            Nom = c.Nom,
            Icone = c.Icone,
            NombreCharges = c.Charges?.Count ?? 0
        }).ToList();
    }
}

public record GetPaiementsChargeQuery(
    int Page = 1, int PageSize = 200,
    string? Search = null
) : IRequest<PagedList<PaiementChargeDto>>;

public class GetPaiementsChargeHandler : IRequestHandler<GetPaiementsChargeQuery, PagedList<PaiementChargeDto>>
{
    private readonly IAppDbContext _db;
    public GetPaiementsChargeHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<PaiementChargeDto>> Handle(GetPaiementsChargeQuery q, CancellationToken ct)
    {
        var query = _db.PaiementsCharge
            .Include(p => p.Charge)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(p => p.Charge.Reference.ToLower().Contains(s) ||
                                     p.Charge.Titre.ToLower().Contains(s));
        }

        query = query.OrderByDescending(p => p.DatePaiement);

        var paged = await PagedList<PaiementCharge>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<PaiementChargeDto>
        {
            Items = paged.Items.Select(p => new PaiementChargeDto
            {
                Id = p.Id,
                ChargeId = p.ChargeId,
                ChargeReference = p.Charge?.Reference ?? "",
                Montant = p.Montant,
                Methode = p.Methode,
                Statut = p.Statut,
                DatePaiement = p.DatePaiement,
                Reference = p.Reference,
                Notes = p.Notes
            }).ToList(),
            TotalCount = paged.TotalCount, Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

// ============ MAPPER ============
public static class ChargeMapper
{
    public static ChargeDto ToDto(Charge c) => new()
    {
        Id = c.Id,
        Reference = c.Reference,
        Titre = c.Titre,
        Description = c.Description,
        Montant = c.Montant,
        MontantPaye = c.MontantPaye,
        Reste = c.Reste,
        ProgressionPaiement = c.ProgressionPaiement,
        Justificatif = c.Justificatif,
        Statut = c.Statut,
        DateCharge = c.DateCharge,
        CategorieChargeId = c.CategorieChargeId,
        NomCategorie = c.CategorieCharge?.Nom ?? "",
        IconeCategorie = c.CategorieCharge?.Icone,
        UtilisateurId = c.UtilisateurId,
        NomUtilisateur = c.Utilisateur?.NomComplet ?? "",
        FournisseurId = c.FournisseurId,
        NomFournisseur = c.Fournisseur?.Nom,
        IconeFournisseur = c.Fournisseur?.Icone,
        Paiements = c.Paiements?.Select(p => new PaiementChargeDto
        {
            Id = p.Id,
            ChargeId = p.ChargeId,
            ChargeReference = c.Reference,
            Montant = p.Montant,
            Methode = p.Methode,
            Statut = p.Statut,
            DatePaiement = p.DatePaiement,
            Reference = p.Reference,
            Notes = p.Notes
        }).ToList() ?? new()
    };
}
