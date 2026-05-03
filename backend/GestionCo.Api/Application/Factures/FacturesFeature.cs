using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Factures;

public class FactureDto
{
    public int Id { get; set; }
    public string NumeroFacture { get; set; } = string.Empty;
    public int VenteId { get; set; }
    public string VenteReference { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string? ClientICE { get; set; }
    public string? ClientInitiales { get; set; }
    public DateTime DateEmission { get; set; }
    public DateTime DateEcheance { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public StatutFacture Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public bool EstEnvoyeeEmail { get; set; }
    public DateTime? DateEnvoiEmail { get; set; }
    public int JoursEcheance { get; set; }
    public bool EstEnRetard => JoursEcheance < 0 && Statut != StatutFacture.Payee && Statut != StatutFacture.Annulee;
}

public record GetFacturesQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    StatutFacture? Statut = null,
    int? ClientId = null
) : IRequest<PagedList<FactureDto>>;

public class GetFacturesHandler : IRequestHandler<GetFacturesQuery, PagedList<FactureDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public GetFacturesHandler(IAppDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<PagedList<FactureDto>> Handle(GetFacturesQuery q, CancellationToken ct)
    {
        var query = _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .AsQueryable();

        // Filtre client si rôle Client
        if (_current.Role == RoleUtilisateur.Client && _current.UserId.HasValue)
        {
            var clientId = await _db.Utilisateurs
                .Where(u => u.Id == _current.UserId.Value)
                .Select(u => u.ClientId).FirstOrDefaultAsync(ct);
            if (clientId.HasValue) query = query.Where(f => f.Vente.ClientId == clientId);
            else query = query.Where(f => false);
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(f =>
                f.NumeroFacture.ToLower().Contains(s) ||
                f.Vente.Reference.ToLower().Contains(s) ||
                f.Vente.Client.NomClient.ToLower().Contains(s));
        }

        if (q.Statut.HasValue) query = query.Where(f => f.Statut == q.Statut);
        if (q.ClientId.HasValue) query = query.Where(f => f.Vente.ClientId == q.ClientId);

        query = query.OrderByDescending(f => f.DateEmission);

        var paged = await PagedList<Facture>.CreateAsync(query, q.Page, q.PageSize, ct);

        return new PagedList<FactureDto>
        {
            Items = paged.Items.Select(FactureMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public record GetFactureByIdQuery(int Id) : IRequest<FactureDto>;

public class GetFactureByIdHandler : IRequestHandler<GetFactureByIdQuery, FactureDto>
{
    private readonly IAppDbContext _db;
    public GetFactureByIdHandler(IAppDbContext db) => _db = db;

    public async Task<FactureDto> Handle(GetFactureByIdQuery q, CancellationToken ct)
    {
        var f = await _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .FirstOrDefaultAsync(f => f.Id == q.Id, ct)
            ?? throw new NotFoundException("Facture", q.Id);
        return FactureMapper.ToDto(f);
    }
}

// Download PDF
public record DownloadFacturePdfQuery(int Id) : IRequest<byte[]>;

public class DownloadFacturePdfHandler : IRequestHandler<DownloadFacturePdfQuery, byte[]>
{
    private readonly IPdfService _pdf;
    public DownloadFacturePdfHandler(IPdfService pdf) => _pdf = pdf;

    public async Task<byte[]> Handle(DownloadFacturePdfQuery q, CancellationToken ct)
        => await _pdf.GenerateInvoicePdfAsync(q.Id, ct);
}

// ============ CREATE FACTURE (manuelle pour vente existante sans facture) ============
public class CreateFactureDto
{
    public int VenteId { get; set; }
    public DateTime? DateEmission { get; set; }
    public DateTime? DateEcheance { get; set; }
}

public record CreateFactureCommand(CreateFactureDto Dto) : IRequest<FactureDto>;

public class CreateFactureHandler : IRequestHandler<CreateFactureCommand, FactureDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly IAuditLogger _audit;

    public CreateFactureHandler(IAppDbContext db, IReferenceGenerator refGen, IAuditLogger audit)
    {
        _db = db;
        _refGen = refGen;
        _audit = audit;
    }

    public async Task<FactureDto> Handle(CreateFactureCommand req, CancellationToken ct)
    {
        var dto = req.Dto;

        var vente = await _db.Ventes
            .Include(v => v.Client)
            .Include(v => v.Facture)
            .FirstOrDefaultAsync(v => v.Id == dto.VenteId, ct)
            ?? throw new NotFoundException("Vente", dto.VenteId);

        if (vente.Facture != null)
            throw new BusinessException($"Cette vente a déjà une facture ({vente.Facture.NumeroFacture})");

        if (vente.Statut == StatutVente.Annule)
            throw new BusinessException("Impossible de créer une facture pour une vente annulée");

        var numero = await _refGen.GenerateInvoiceReferenceAsync(ct);
        var now = DateTime.UtcNow;
        var dateEmission = dto.DateEmission ?? now;
        var dateEcheance = dto.DateEcheance ?? dateEmission.AddDays(30);

        var facture = new Facture
        {
            NumeroFacture = numero,
            VenteId = vente.Id,
            DateEmission = dateEmission,
            DateEcheance = dateEcheance,
            Statut = vente.MontantPaye >= vente.MontantTotal ? StatutFacture.Payee : 
                     (vente.MontantPaye > 0 ? StatutFacture.PartiellementPayee : StatutFacture.EnAttente)
        };

        _db.Factures.Add(facture);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            ActionLog.Create, "factures",
            $"Facture créée manuellement : {numero} pour vente {vente.Reference}",
            facture.Id, numero, ct: ct);

        return await GetDetails(facture.Id, ct);
    }

    private async Task<FactureDto> GetDetails(int id, CancellationToken ct)
    {
        var f = await _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .FirstAsync(f => f.Id == id, ct);
        return FactureMapper.ToDto(f);
    }
}

// ============ GET VENTES SANS FACTURE (pour le sélecteur) ============
public record GetVentesSansFactureQuery : IRequest<List<VenteSansFactureDto>>;

public class VenteSansFactureDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime DateVente { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public decimal MontantTotal { get; set; }
}

public class GetVentesSansFactureHandler : IRequestHandler<GetVentesSansFactureQuery, List<VenteSansFactureDto>>
{
    private readonly IAppDbContext _db;
    public GetVentesSansFactureHandler(IAppDbContext db) => _db = db;

    public async Task<List<VenteSansFactureDto>> Handle(GetVentesSansFactureQuery q, CancellationToken ct)
    {
        return await _db.Ventes
            .Where(v => v.Facture == null && v.Statut != StatutVente.Annule)
            .Include(v => v.Client)
            .OrderByDescending(v => v.DateVente)
            .Take(100)
            .Select(v => new VenteSansFactureDto
            {
                Id = v.Id,
                Reference = v.Reference,
                DateVente = v.DateVente,
                NomClient = v.Client.NomClient,
                MontantTotal = v.MontantTotal
            })
            .ToListAsync(ct);
    }
}

public static class FactureMapper
{
    public static FactureDto ToDto(Facture f)
    {
        var statut = f.Statut;
        if (statut == StatutFacture.EnAttente && f.Vente != null && f.Vente.MontantPaye > 0 && f.Vente.MontantPaye < f.Vente.MontantTotal)
        {
            statut = StatutFacture.PartiellementPayee;
        }

        return new FactureDto
        {
            Id = f.Id,
            NumeroFacture = f.NumeroFacture,
            VenteId = f.VenteId,
            VenteReference = f.Vente?.Reference ?? "",
            ClientId = f.Vente?.ClientId ?? 0,
            NomClient = f.Vente?.Client?.NomClient ?? "",
            ClientICE = f.Vente?.Client?.ICE,
            ClientInitiales = f.Vente?.Client?.Initiales,
            DateEmission = f.DateEmission,
            DateEcheance = f.DateEcheance,
            MontantTotal = f.Vente?.MontantTotal ?? 0,
            MontantPaye = f.Vente?.MontantPaye ?? 0,
            Statut = statut,
            EstEnvoyeeEmail = f.EstEnvoyeeEmail,
            DateEnvoiEmail = f.DateEnvoiEmail,
            JoursEcheance = (f.DateEcheance - DateTime.UtcNow).Days
        };
    }
}
