using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Paiements;

public class PaiementDto
{
    public int Id { get; set; }
    public int VenteId { get; set; }
    public string VenteReference { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string? ClientInitiales { get; set; }
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public string MethodeLibelle => Methode.ToString();
    public StatutPaiement Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public DateTime DatePaiement { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class CreatePaiementDto
{
    public int VenteId { get; set; }
    public decimal Montant { get; set; }
    public MethodePaiement Methode { get; set; }
    public DateTime? DatePaiement { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public record CreatePaiementCommand(CreatePaiementDto Dto) : IRequest<PaiementDto>;

public class CreatePaiementValidator : AbstractValidator<CreatePaiementCommand>
{
    public CreatePaiementValidator()
    {
        RuleFor(x => x.Dto.VenteId).GreaterThan(0);
        RuleFor(x => x.Dto.Montant).GreaterThan(0);
    }
}

public class CreatePaiementHandler : IRequestHandler<CreatePaiementCommand, PaiementDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public CreatePaiementHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db; _audit = audit;
    }

    public async Task<PaiementDto> Handle(CreatePaiementCommand req, CancellationToken ct)
    {
        var vente = await _db.Ventes
            .Include(v => v.Client)
            .Include(v => v.Facture)
            .FirstOrDefaultAsync(v => v.Id == req.Dto.VenteId, ct)
            ?? throw new NotFoundException("Vente", req.Dto.VenteId);

        if (vente.Statut == StatutVente.Annule)
            throw new BusinessException("Impossible d'ajouter un paiement à une vente annulée");

        if (req.Dto.Montant > vente.Reste)
            throw new BusinessException($"Montant supérieur au reste dû ({vente.Reste:N2} MAD)");

        var paiement = new Paiement
        {
            VenteId = req.Dto.VenteId,
            Montant = req.Dto.Montant,
            Methode = req.Dto.Methode,
            Statut = StatutPaiement.Confirme,
            DatePaiement = req.Dto.DatePaiement ?? DateTime.UtcNow,
            Reference = req.Dto.Reference,
            Notes = req.Dto.Notes
        };

        _db.Paiements.Add(paiement);
        vente.MontantPaye += req.Dto.Montant;

        // Si totalement payée → statut payé + facture payée
        if (vente.MontantPaye >= vente.MontantTotal)
        {
            vente.Statut = StatutVente.Paye;
            if (vente.Facture != null)
                vente.Facture.Statut = StatutFacture.Payee;
        }
        // Si partiellement payée → mettre à jour le statut de la facture
        else if (vente.MontantPaye > 0 && vente.Facture != null)
        {
            vente.Facture.Statut = StatutFacture.PartiellementPayee;
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "paiements",
            $"Paiement de {paiement.Montant:N2} MAD pour {vente.Reference} ({vente.Client.NomClient})",
            paiement.Id, vente.Reference, ct: ct);

        return await GetDetails(paiement.Id, ct);
    }

    private async Task<PaiementDto> GetDetails(int id, CancellationToken ct)
    {
        var p = await _db.Paiements
            .Include(p => p.Vente).ThenInclude(v => v.Client)
            .FirstAsync(p => p.Id == id, ct);
        return PaiementMapper.ToDto(p);
    }
}

// ============ QUERIES ============
public record GetPaiementsQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    StatutPaiement? Statut = null,
    MethodePaiement? Methode = null
) : IRequest<PagedList<PaiementDto>>;

public class GetPaiementsHandler : IRequestHandler<GetPaiementsQuery, PagedList<PaiementDto>>
{
    private readonly IAppDbContext _db;
    public GetPaiementsHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<PaiementDto>> Handle(GetPaiementsQuery q, CancellationToken ct)
    {
        var query = _db.Paiements
            .Include(p => p.Vente).ThenInclude(v => v.Client)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(p =>
                p.Vente.Reference.ToLower().Contains(s) ||
                p.Vente.Client.NomClient.ToLower().Contains(s));
        }

        if (q.Statut.HasValue) query = query.Where(p => p.Statut == q.Statut);
        if (q.Methode.HasValue) query = query.Where(p => p.Methode == q.Methode);

        query = query.OrderByDescending(p => p.DatePaiement);

        var paged = await PagedList<Paiement>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<PaiementDto>
        {
            Items = paged.Items.Select(PaiementMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page, PageSize = paged.PageSize
        };
    }
}

public static class PaiementMapper
{
    public static PaiementDto ToDto(Paiement p) => new()
    {
        Id = p.Id, VenteId = p.VenteId,
        VenteReference = p.Vente?.Reference ?? "",
        ClientId = p.Vente?.ClientId ?? 0,
        NomClient = p.Vente?.Client?.NomClient ?? "",
        ClientInitiales = p.Vente?.Client?.Initiales,
        Montant = p.Montant,
        Methode = p.Methode, Statut = p.Statut,
        DatePaiement = p.DatePaiement,
        Reference = p.Reference, Notes = p.Notes
    };
}
