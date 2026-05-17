using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Clients;

// ============ DTOs ============
public class ClientDto
{
    public int Id { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public TypeClient Type { get; set; }
    public string? ICE { get; set; }
    public string? RC { get; set; }
    public string? IF { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? PersonneContact { get; set; }
    public bool IsActive { get; set; }
    public string? SourceAcquisition { get; set; }
    public string Initiales { get; set; } = "??";
    public int NombreCommandes { get; set; }
    public decimal TotalDepense { get; set; }
    public decimal TotalImpaye { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UtilisateurId { get; set; }
}

public class CreateClientDto
{
    public string NomClient { get; set; } = string.Empty;
    public TypeClient Type { get; set; } = TypeClient.Entreprise;
    public string? ICE { get; set; }
    public string? RC { get; set; }
    public string? IF { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? PersonneContact { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SourceAcquisition { get; set; }
    public bool CreerCompte { get; set; } = false;
    public string? MotDePasse { get; set; }
}

public class UpdateClientDto : CreateClientDto
{
    public int Id { get; set; }
}

// ============ COMMANDS ============
public record CreateClientCommand(CreateClientDto Dto) : IRequest<ClientDto>;

public class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.Dto.NomClient).NotEmpty().MaximumLength(200);
        When(x => x.Dto.Type == TypeClient.Entreprise, () =>
        {
            RuleFor(x => x.Dto.ICE).NotEmpty().Length(15).Matches(@"^\d{15}$")
                .WithMessage("ICE doit contenir exactement 15 chiffres");
        });
        When(x => x.Dto.CreerCompte, () =>
        {
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Dto.MotDePasse).NotEmpty().MinimumLength(8);
        });
    }
}

public class CreateClientHandler : IRequestHandler<CreateClientCommand, ClientDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;

    public CreateClientHandler(IAppDbContext db, IPasswordHasher hasher, IAuditLogger audit)
    {
        _db = db; _hasher = hasher; _audit = audit;
    }

    public async Task<ClientDto> Handle(CreateClientCommand req, CancellationToken ct)
    {
        var dto = req.Dto;

        Utilisateur? user = null;
        if (dto.CreerCompte && !string.IsNullOrEmpty(dto.Email) && !string.IsNullOrEmpty(dto.MotDePasse))
        {
            if (await _db.Utilisateurs.AnyAsync(u => u.Email == dto.Email.ToLower(), ct))
                throw new BusinessException($"Email déjà utilisé : {dto.Email}");

            var parts = dto.NomClient.Split(' ', 2);
            user = new Utilisateur
            {
                Nom = parts.Length > 1 ? parts[1] : dto.NomClient,
                Prenom = parts.Length > 1 ? parts[0] : "",
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = _hasher.Hash(dto.MotDePasse),
                Telephone = dto.Telephone,
                Role = RoleUtilisateur.Client,
                IsActive = true
            };
            _db.Utilisateurs.Add(user);
            await _db.SaveChangesAsync(ct);
        }

        var client = new Client
        {
            NomClient = dto.NomClient.Trim(),
            Type = dto.Type,
            ICE = dto.ICE?.Trim(),
            RC = dto.RC?.Trim(),
            IF = dto.IF?.Trim(),
            Adresse = dto.Adresse?.Trim(),
            Ville = dto.Ville?.Trim(),
            Telephone = dto.Telephone?.Trim(),
            Email = dto.Email?.Trim().ToLower(),
            PersonneContact = dto.PersonneContact?.Trim(),
            IsActive = dto.IsActive,
            SourceAcquisition = dto.SourceAcquisition?.Trim(),
            UtilisateurId = user?.Id
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);

        if (user != null)
        {
            user.ClientId = client.Id;
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync(ActionLog.Create, "clients",
            $"Client créé : {client.NomClient}", client.Id, ct: ct);

        return ClientMapper.ToDto(client);
    }
}

public record UpdateClientCommand(UpdateClientDto Dto) : IRequest<ClientDto>;

public class UpdateClientHandler : IRequestHandler<UpdateClientCommand, ClientDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateClientHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<ClientDto> Handle(UpdateClientCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == dto.Id, ct)
            ?? throw new NotFoundException("Client", dto.Id);

        client.NomClient = dto.NomClient.Trim();
        client.Type = dto.Type;
        client.ICE = dto.ICE?.Trim();
        client.RC = dto.RC?.Trim();
        client.IF = dto.IF?.Trim();
        client.Adresse = dto.Adresse?.Trim();
        client.Ville = dto.Ville?.Trim();
        client.Telephone = dto.Telephone?.Trim();
        client.Email = dto.Email?.Trim().ToLower();
        client.PersonneContact = dto.PersonneContact?.Trim();
        client.IsActive = dto.IsActive;
        client.SourceAcquisition = dto.SourceAcquisition?.Trim();

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "clients",
            $"Client modifié : {client.NomClient}", client.Id, ct: ct);

        return ClientMapper.ToDto(client);
    }
}

public record DeleteClientCommand(int Id) : IRequest<Unit>;

public class DeleteClientHandler : IRequestHandler<DeleteClientCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteClientHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<Unit> Handle(DeleteClientCommand req, CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new NotFoundException("Client", req.Id);

        var hasVentes = await _db.Ventes.AnyAsync(v => v.ClientId == req.Id, ct);
        if (hasVentes)
            throw new BusinessException("Ce client ne peut pas être supprimé car il possède des ventes associées. Désactivez-le à la place.");

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Delete, "clients",
            $"Client supprimé : {client.NomClient}",
            client.Id, estSensible: true, ct: ct);

        return Unit.Value;
    }
}

// ============ BULK IMPORT ============
public record BulkImportClientsCommand(List<CreateClientDto> Items) : IRequest<BulkImportResultDto>;

public class BulkImportClientsHandler : IRequestHandler<BulkImportClientsCommand, BulkImportResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public BulkImportClientsHandler(IAppDbContext db, IAuditLogger audit) { _db = db; _audit = audit; }

    public async Task<BulkImportResultDto> Handle(BulkImportClientsCommand req, CancellationToken ct)
    {
        var result = new BulkImportResultDto();
        if (req.Items.Count == 0) return result;
        if (req.Items.Count > 500) throw new BusinessException("Maximum 500 lignes par import");

        var toAdd = new List<Client>();

        for (int i = 0; i < req.Items.Count; i++)
        {
            var dto = req.Items[i];
            var row = i + 2;

            if (string.IsNullOrWhiteSpace(dto.NomClient))
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom client requis" }); continue; }
            if (dto.NomClient.Length > 200)
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom trop long (max 200 caractères)" }); continue; }
            if (dto.Type == TypeClient.Entreprise && !string.IsNullOrWhiteSpace(dto.ICE) && dto.ICE.Length != 15)
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "ICE doit contenir exactement 15 chiffres" }); continue; }

            toAdd.Add(new Client
            {
                NomClient = dto.NomClient.Trim(),
                Type = dto.Type,
                ICE = dto.ICE?.Trim(),
                RC = dto.RC?.Trim(),
                IF = dto.IF?.Trim(),
                Adresse = dto.Adresse?.Trim(),
                Ville = dto.Ville?.Trim(),
                Telephone = dto.Telephone?.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim().ToLower(),
                PersonneContact = dto.PersonneContact?.Trim(),
                IsActive = true,
                SourceAcquisition = dto.SourceAcquisition?.Trim(),
            });
        }

        if (toAdd.Count > 0)
        {
            _db.Clients.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);
            await _audit.LogAsync(ActionLog.Create, "clients",
                $"Import massif : {toAdd.Count} client(s) créé(s)", ct: ct);
        }

        result.Imported = toAdd.Count;
        result.Failed = result.Errors.Count;
        return result;
    }
}

// ============ QUERIES ============
public record GetClientsQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    TypeClient? Type = null,
    bool? IsActive = null
) : IRequest<PagedList<ClientDto>>;

public class GetClientsHandler : IRequestHandler<GetClientsQuery, PagedList<ClientDto>>
{
    private readonly IAppDbContext _db;
    public GetClientsHandler(IAppDbContext db) => _db = db;

    public async Task<PagedList<ClientDto>> Handle(GetClientsQuery q, CancellationToken ct)
    {
        var query = _db.Clients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(c =>
                c.NomClient.ToLower().Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.ICE != null && c.ICE.Contains(s)));
        }

        if (q.Type.HasValue) query = query.Where(c => c.Type == q.Type);
        if (q.IsActive.HasValue) query = query.Where(c => c.IsActive == q.IsActive);

        query = query.OrderByDescending(c => c.CreatedAt);

        var paged = await PagedList<Client>.CreateAsync(query, q.Page, q.PageSize, ct);

        // Enrichir avec les stats
        var clientIds = paged.Items.Select(c => c.Id).ToList();
        var ventesData = await _db.Ventes
            .Where(v => clientIds.Contains(v.ClientId))
            .GroupBy(v => v.ClientId)
            .Select(g => new
            {
                ClientId = g.Key,
                Count = g.Count(),
                Total = g.Sum(v => v.MontantTotal),
                Impaye = g.Sum(v => v.MontantTotal - v.MontantPaye)
            })
            .ToListAsync(ct);

        var items = paged.Items.Select(c =>
        {
            var dto = ClientMapper.ToDto(c);
            var stats = ventesData.FirstOrDefault(v => v.ClientId == c.Id);
            if (stats != null)
            {
                dto.NombreCommandes = stats.Count;
                dto.TotalDepense = stats.Total;
                dto.TotalImpaye = stats.Impaye;
            }
            return dto;
        }).ToList();

        return new PagedList<ClientDto>
        {
            Items = items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}

public record GetClientByIdQuery(int Id) : IRequest<ClientDto>;

public class GetClientByIdHandler : IRequestHandler<GetClientByIdQuery, ClientDto>
{
    private readonly IAppDbContext _db;
    public GetClientByIdHandler(IAppDbContext db) => _db = db;

    public async Task<ClientDto> Handle(GetClientByIdQuery q, CancellationToken ct)
    {
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == q.Id, ct)
            ?? throw new NotFoundException("Client", q.Id);
        return ClientMapper.ToDto(client);
    }
}

// ============ MAPPER ============
public static class ClientMapper
{
    public static ClientDto ToDto(Client c) => new()
    {
        Id = c.Id, NomClient = c.NomClient, Type = c.Type,
        ICE = c.ICE, RC = c.RC, IF = c.IF,
        Adresse = c.Adresse, Ville = c.Ville, Telephone = c.Telephone,
        Email = c.Email, PersonneContact = c.PersonneContact,
        IsActive = c.IsActive, SourceAcquisition = c.SourceAcquisition,
        Initiales = c.Initiales, CreatedAt = c.CreatedAt, UtilisateurId = c.UtilisateurId
    };
}

public record ClientsStatsDto(int Total, int Actifs, decimal CA, decimal Impayes);
public record GetClientsStatsQuery() : IRequest<ClientsStatsDto>;

public class GetClientsStatsHandler(IAppDbContext db) : IRequestHandler<GetClientsStatsQuery, ClientsStatsDto>
{
    public async Task<ClientsStatsDto> Handle(GetClientsStatsQuery _, CancellationToken ct)
    {
        var total  = await db.Clients.CountAsync(ct);
        var actifs = await db.Clients.CountAsync(c => c.IsActive, ct);
        var ca = await db.Ventes
            .Where(v => v.Statut != StatutVente.Annule)
            .SumAsync(v => (decimal?)v.MontantTotal, ct) ?? 0;
        var impayes = await db.Factures
            .Where(f => f.Statut != StatutFacture.Payee && f.Statut != StatutFacture.Annulee)
            .SumAsync(f => (decimal?)(f.Vente.MontantTotal - f.Vente.MontantPaye), ct) ?? 0;
        return new ClientsStatsDto(total, actifs, ca, impayes);
    }
}
