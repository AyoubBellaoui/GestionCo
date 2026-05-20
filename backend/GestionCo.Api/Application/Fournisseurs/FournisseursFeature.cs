using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;


namespace GestionCo.Api.Application.Fournisseurs;

public class FournisseurDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? SiteWeb { get; set; }
    public string? PersonneContact { get; set; }
    public bool IsActive { get; set; }
    public int NombreProduits { get; set; }
    public int NombreAchats { get; set; }
    public decimal MontantTotalAchats { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFournisseurDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; } = "📦";
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? SiteWeb { get; set; }
    public string? PersonneContact { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateFournisseurDto : CreateFournisseurDto
{
    public int Id { get; set; }
}

// ============ COMMANDS ============
public record CreateFournisseurCommand(CreateFournisseurDto Dto) : IRequest<FournisseurDto>;
public record UpdateFournisseurCommand(UpdateFournisseurDto Dto) : IRequest<FournisseurDto>;
public record DeleteFournisseurCommand(int Id) : IRequest<Unit>;

public class FournisseurValidator : AbstractValidator<CreateFournisseurCommand>
{
    public FournisseurValidator()
    {
        RuleFor(x => x.Dto.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.Telephone).NotEmpty();
    }
}

public class CreateFournisseurHandler : IRequestHandler<CreateFournisseurCommand, FournisseurDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    public CreateFournisseurHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<FournisseurDto> Handle(CreateFournisseurCommand req, CancellationToken ct)
    {
        var f = new Fournisseur
        {
            Nom = req.Dto.Nom.Trim(),
            Icone = req.Dto.Icone,
            Telephone = req.Dto.Telephone?.Trim(),
            Email = req.Dto.Email?.Trim().ToLower(),
            Adresse = req.Dto.Adresse?.Trim(),
            SiteWeb = req.Dto.SiteWeb?.Trim(),
            PersonneContact = req.Dto.PersonneContact?.Trim(),
            IsActive = req.Dto.IsActive
        };
        _db.Fournisseurs.Add(f);
        await _db.SaveChangesAsync(ct);

        var fDetails = new List<string>();
        if (!string.IsNullOrEmpty(f.Telephone)) fDetails.Add($"Tél: {f.Telephone}");
        if (!string.IsNullOrEmpty(f.Email)) fDetails.Add(f.Email);
        if (!string.IsNullOrEmpty(f.PersonneContact)) fDetails.Add($"Contact: {f.PersonneContact}");
        await _audit.LogAsync(ActionLog.Create, "fournisseurs",
            $"Fournisseur créé : {f.Nom}" + (fDetails.Count > 0 ? $" — {string.Join(" · ", fDetails)}" : ""),
            f.Id, ct: ct);

        return FournisseurMapper.ToDto(f);
    }
}

public class UpdateFournisseurHandler : IRequestHandler<UpdateFournisseurCommand, FournisseurDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    public UpdateFournisseurHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<FournisseurDto> Handle(UpdateFournisseurCommand req, CancellationToken ct)
    {
        var f = await _db.Fournisseurs.FirstOrDefaultAsync(x => x.Id == req.Dto.Id, ct)
            ?? throw new NotFoundException("Fournisseur", req.Dto.Id);

        f.Nom = req.Dto.Nom.Trim();
        f.Icone = req.Dto.Icone;
        f.Telephone = req.Dto.Telephone?.Trim();
        f.Email = req.Dto.Email?.Trim().ToLower();
        f.Adresse = req.Dto.Adresse?.Trim();
        f.SiteWeb = req.Dto.SiteWeb?.Trim();
        f.PersonneContact = req.Dto.PersonneContact?.Trim();
        f.IsActive = req.Dto.IsActive;
        await _db.SaveChangesAsync(ct);

        var fUpdDetails = new List<string>();
        if (!string.IsNullOrEmpty(f.Telephone)) fUpdDetails.Add($"Tél: {f.Telephone}");
        if (!string.IsNullOrEmpty(f.Email)) fUpdDetails.Add(f.Email);
        if (!string.IsNullOrEmpty(f.PersonneContact)) fUpdDetails.Add($"Contact: {f.PersonneContact}");
        await _audit.LogAsync(ActionLog.Update, "fournisseurs",
            $"Fournisseur modifié : {f.Nom}" + (fUpdDetails.Count > 0 ? $" — {string.Join(" · ", fUpdDetails)}" : ""),
            f.Id, ct: ct);

        return FournisseurMapper.ToDto(f);
    }
}

public class DeleteFournisseurHandler : IRequestHandler<DeleteFournisseurCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    public DeleteFournisseurHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<Unit> Handle(DeleteFournisseurCommand req, CancellationToken ct)
    {
        var f = await _db.Fournisseurs.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Fournisseur", req.Id);

        var hasUsage = await _db.Achats.AnyAsync(a => a.FournisseurId == req.Id, ct)
                       || await _db.Produits.AnyAsync(p => p.FournisseurId == req.Id, ct);

        if (hasUsage)
        {
            f.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            _db.Fournisseurs.Remove(f);
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync(ActionLog.Delete, "fournisseurs",
            $"Fournisseur {(hasUsage ? "désactivé" : "supprimé")} : {f.Nom}",
            f.Id, estSensible: true, ct: ct);

        return Unit.Value;
    }
}

// ============ BULK IMPORT ============
public record BulkImportFournisseursCommand(List<CreateFournisseurDto> Items) : IRequest<BulkImportResultDto>;

public class BulkImportFournisseursHandler : IRequestHandler<BulkImportFournisseursCommand, BulkImportResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public BulkImportFournisseursHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<BulkImportResultDto> Handle(BulkImportFournisseursCommand req, CancellationToken ct)
    {
        var result = new BulkImportResultDto();
        if (req.Items.Count == 0) return result;
        if (req.Items.Count > 500) throw new BusinessException("Maximum 500 lignes par import");

        var toAdd = new List<Fournisseur>();

        for (int i = 0; i < req.Items.Count; i++)
        {
            var dto = req.Items[i];
            var row = i + 2;

            if (string.IsNullOrWhiteSpace(dto.Nom))
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom requis" }); continue; }
            if (dto.Nom.Length > 200)
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom trop long (max 200 caractères)" }); continue; }
            if (string.IsNullOrWhiteSpace(dto.Telephone))
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Téléphone requis" }); continue; }

            toAdd.Add(new Fournisseur
            {
                Nom = dto.Nom.Trim(),
                Icone = string.IsNullOrWhiteSpace(dto.Icone) ? "📦" : dto.Icone,
                Telephone = dto.Telephone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower(),
                Adresse = dto.Adresse?.Trim(),
                SiteWeb = dto.SiteWeb?.Trim(),
                PersonneContact = dto.PersonneContact?.Trim(),
                IsActive = true,
            });
        }

        if (toAdd.Count > 0)
        {
            _db.Fournisseurs.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(ActionLog.Create, "fournisseurs",
                $"Import massif : {toAdd.Count} fournisseur(s) créé(s)", ct: ct);
        }

        result.Imported = toAdd.Count;
        result.Failed = result.Errors.Count;
        return result;
    }
}

// ============ QUERIES ============
public record GetFournisseursQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    bool? IsActive = null
) : IRequest<PagedList<FournisseurDto>>;

public class GetFournisseursHandler : IRequestHandler<GetFournisseursQuery, PagedList<FournisseurDto>>
{
    private readonly IAppDbContext _db;
    public GetFournisseursHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<FournisseurDto>> Handle(GetFournisseursQuery q, CancellationToken ct)
    {
        var query = _db.Fournisseurs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(f => f.Nom.ToLower().Contains(s) ||
                                     (f.Email != null && f.Email.ToLower().Contains(s)));
        }

        if (q.IsActive.HasValue) query = query.Where(f => f.IsActive == q.IsActive);

        query = query.OrderBy(f => f.Nom);

        var paged = await PagedList<Fournisseur>.CreateAsync(query, q.Page, q.PageSize, ct);

        var fournIds = paged.Items.Select(f => f.Id).ToList();
        var stats = await _db.Achats
            .Where(a => fournIds.Contains(a.FournisseurId))
            .GroupBy(a => a.FournisseurId)
            .Select(g => new { FournId = g.Key, Count = g.Count(), Total = g.Sum(x => x.MontantTotal) })
            .ToListAsync(ct);

        var prodCounts = await _db.Produits
            .Where(p => p.FournisseurId != null && fournIds.Contains(p.FournisseurId.Value))
            .GroupBy(p => p.FournisseurId!.Value)
            .Select(g => new { FournId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var items = paged.Items.Select(f =>
        {
            var dto = FournisseurMapper.ToDto(f);
            var s = stats.FirstOrDefault(x => x.FournId == f.Id);
            var p = prodCounts.FirstOrDefault(x => x.FournId == f.Id);
            if (s != null) { dto.NombreAchats = s.Count; dto.MontantTotalAchats = s.Total; }
            if (p != null) { dto.NombreProduits = p.Count; }
            return dto;
        }).ToList();

        return new PagedList<FournisseurDto>
        {
            Items = items, TotalCount = paged.TotalCount,
            Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public record GetFournisseurByIdQuery(int Id) : IRequest<FournisseurDto>;

public class GetFournisseurByIdHandler : IRequestHandler<GetFournisseurByIdQuery, FournisseurDto>
{
    private readonly IAppDbContext _db;
    public GetFournisseurByIdHandler(IAppDbContext db) => _db = db;

    public async Task<FournisseurDto> Handle(GetFournisseurByIdQuery q, CancellationToken ct)
    {
        var f = await _db.Fournisseurs.FirstOrDefaultAsync(x => x.Id == q.Id, ct)
            ?? throw new NotFoundException("Fournisseur", q.Id);
        return FournisseurMapper.ToDto(f);
    }
}

public static class FournisseurMapper
{
    public static FournisseurDto ToDto(Fournisseur f) => new()
    {
        Id = f.Id, Nom = f.Nom, Icone = f.Icone,
        Telephone = f.Telephone, Email = f.Email,
        Adresse = f.Adresse, SiteWeb = f.SiteWeb,
        PersonneContact = f.PersonneContact,
        IsActive = f.IsActive, CreatedAt = f.CreatedAt
    };
}

public record FournisseursStatsDto(int Total, int Produits, int Commandes, decimal Achats);
public record GetFournisseursStatsQuery() : IRequest<FournisseursStatsDto>;

public class GetFournisseursStatsHandler(IAppDbContext db) : IRequestHandler<GetFournisseursStatsQuery, FournisseursStatsDto>
{
    public async Task<FournisseursStatsDto> Handle(GetFournisseursStatsQuery _, CancellationToken ct)
    {
        var total     = await db.Fournisseurs.CountAsync(ct);
        var produits  = await db.Produits.CountAsync(p => p.FournisseurId != null, ct);
        var commandes = await db.Achats.CountAsync(ct);
        var achats    = await db.Achats.SumAsync(a => (decimal?)a.MontantTotal, ct) ?? 0;
        return new FournisseursStatsDto(total, produits, commandes, achats);
    }
}
