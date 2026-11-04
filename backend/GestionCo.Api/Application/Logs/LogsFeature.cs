using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Logs;

public class LogDto
{
    public int Id { get; set; }
    public int? UtilisateurId { get; set; }
    public string? NomUtilisateur { get; set; }
    public ActionLog Action { get; set; }
    public string Entite { get; set; } = string.Empty;
    public string? EntiteReference { get; set; }
    public int? EntiteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AnciennesValeurs { get; set; }
    public string? NouvellesValeurs { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool EstSensible { get; set; }
    public DateTime DateAction { get; set; }
}

public record GetLogsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    ActionLog? Action = null,
    string? Entite = null,
    int? UtilisateurId = null,
    bool? EstSensible = null,
    DateTime? DateDebut = null,
    DateTime? DateFin = null
) : IRequest<PagedList<LogDto>>;

public class GetLogsHandler : IRequestHandler<GetLogsQuery, PagedList<LogDto>>
{
    private readonly IAppDbContext _db;
    public GetLogsHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<LogDto>> Handle(GetLogsQuery q, CancellationToken ct)
    {
        var query = _db.Logs
            .Include(l => l.Utilisateur)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(l =>
                l.Description.ToLower().Contains(s) ||
                l.Entite.ToLower().Contains(s) ||
                (l.EntiteReference != null && l.EntiteReference.ToLower().Contains(s)) ||
                (l.Utilisateur != null && l.Utilisateur.NomComplet.ToLower().Contains(s)));
        }

        if (q.Action.HasValue) query = query.Where(l => l.Action == q.Action.Value);
        if (!string.IsNullOrWhiteSpace(q.Entite)) query = query.Where(l => l.Entite == q.Entite);
        if (q.UtilisateurId.HasValue) query = query.Where(l => l.UtilisateurId == q.UtilisateurId);
        if (q.EstSensible.HasValue) query = query.Where(l => l.EstSensible == q.EstSensible.Value);
        if (q.DateDebut.HasValue) query = query.Where(l => l.DateAction >= q.DateDebut.Value.Date);
        if (q.DateFin.HasValue) query = query.Where(l => l.DateAction < q.DateFin.Value.Date.AddDays(1));

        query = query.OrderByDescending(l => l.DateAction);

        var paged = await PagedList<Log>.CreateAsync(query, q.Page, q.PageSize, ct);
        return new PagedList<LogDto>
        {
            Items = paged.Items.Select(ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }

    private static LogDto ToDto(Log l) => new()
    {
        Id = l.Id,
        UtilisateurId = l.UtilisateurId,
        NomUtilisateur = l.Utilisateur?.NomComplet,
        Action = l.Action,
        Entite = l.Entite,
        EntiteReference = l.EntiteReference,
        EntiteId = l.EntiteId,
        Description = l.Description,
        AnciennesValeurs = l.AnciennesValeurs,
        NouvellesValeurs = l.NouvellesValeurs,
        IpAddress = l.IpAddress,
        UserAgent = l.UserAgent,
        EstSensible = l.EstSensible,
        DateAction = l.DateAction
    };
}

public record LogsStatsDto(int Total, int Sensitifs, int Creations, int Modifications, int Suppressions);
public record GetLogsStatsQuery() : IRequest<LogsStatsDto>;

public class GetLogsStatsHandler(IAppDbContext db) : IRequestHandler<GetLogsStatsQuery, LogsStatsDto>
{
    public async Task<LogsStatsDto> Handle(GetLogsStatsQuery _, CancellationToken ct)
    {
        var total       = await db.Logs.CountAsync(ct);
        var sensitifs   = await db.Logs.CountAsync(l => l.EstSensible, ct);
        var creations   = await db.Logs.CountAsync(l => l.Action == ActionLog.Create, ct);
        var modifs      = await db.Logs.CountAsync(l => l.Action == ActionLog.Update, ct);
        var suppressions = await db.Logs.CountAsync(l => l.Action == ActionLog.Delete, ct);
        return new LogsStatsDto(total, sensitifs, creations, modifs, suppressions);
    }
}
