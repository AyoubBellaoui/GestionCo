using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Ventes;

// ============ DTOs ============
public class VenteDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string? ClientICE { get; set; }
    public string? ClientInitiales { get; set; }
    public int UtilisateurId { get; set; }
    public string NomUtilisateur { get; set; } = string.Empty;
    public DateTime DateVente { get; set; }
    public DateTime? DateEcheance { get; set; }
    public decimal MontantTotalHT { get; set; }
    public decimal MontantTVA { get; set; }
    public decimal MontantTotal { get; set; }
    public decimal MontantPaye { get; set; }
    public decimal Reste { get; set; }
    public int ProgressionPaiement { get; set; }
    public StatutVente Statut { get; set; }
    public string StatutLibelle => Statut.ToString();
    public int NombreArticles { get; set; }
    public List<LigneVenteDto> Lignes { get; set; } = new();
    public string? NumeroFacture { get; set; }
    public int? FactureId { get; set; }
}

public class LigneVenteDto
{
    public int Id { get; set; }
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string ReferenceProduit { get; set; } = string.Empty;
    public string? ImageProduit { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal TVA { get; set; }
    public decimal Total { get; set; }
}

public class CreateVenteDto
{
    public int ClientId { get; set; }
    public DateTime? DateVente { get; set; }
    public DateTime? DateEcheance { get; set; }
    public List<CreateLigneVenteDto> Lignes { get; set; } = new();
    public decimal? PaiementInitial { get; set; } // pour créer auto un paiement
    public MethodePaiement? MethodePaiementInitial { get; set; }
}

public class CreateLigneVenteDto
{
    public int ProduitId { get; set; }
    public int Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal TVA { get; set; } = 20;
}

// ============ COMMANDS ============
public record CreateVenteCommand(CreateVenteDto Dto) : IRequest<VenteDto>;

public class CreateVenteValidator : AbstractValidator<CreateVenteCommand>
{
    public CreateVenteValidator()
    {
        RuleFor(x => x.Dto.ClientId).GreaterThan(0).WithMessage("Client requis");
        RuleFor(x => x.Dto.Lignes).NotEmpty().WithMessage("Au moins une ligne de vente requise");
        RuleForEach(x => x.Dto.Lignes).ChildRules(l =>
        {
            l.RuleFor(x => x.ProduitId).GreaterThan(0);
            l.RuleFor(x => x.Quantite).GreaterThan(0);
            l.RuleFor(x => x.PrixUnitaire).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateVenteHandler : IRequestHandler<CreateVenteCommand, VenteDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public CreateVenteHandler(IAppDbContext db, IReferenceGenerator refGen,
        ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _refGen = refGen; _current = current; _audit = audit;
    }

    public async Task<VenteDto> Handle(CreateVenteCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var userId = _current.UserId ?? throw new UnauthorizedException();

        // Vérifier client
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.Id == dto.ClientId, ct)
            ?? throw new NotFoundException("Client", dto.ClientId);

        // Vérifier stock disponible
        var produitIds = dto.Lignes.Select(l => l.ProduitId).ToList();
        var produits = await _db.Produits
            .Where(p => produitIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var ligne in dto.Lignes)
        {
            var produit = produits.FirstOrDefault(p => p.Id == ligne.ProduitId)
                ?? throw new NotFoundException("Produit", ligne.ProduitId);

            if (produit.QuantiteStock < ligne.Quantite)
                throw new BusinessException(
                    $"Stock insuffisant pour '{produit.Nom}'. Stock disponible : {produit.QuantiteStock}, demandé : {ligne.Quantite}");
        }

        // Créer la vente
        var reference = await _refGen.GenerateSaleReferenceAsync(ct);
        var now = DateTime.UtcNow;

        var vente = new Vente
        {
            Reference = reference,
            ClientId = dto.ClientId,
            UtilisateurId = userId,
            DateVente = dto.DateVente ?? now,
            DateEcheance = dto.DateEcheance ?? now.AddDays(30),
            Statut = StatutVente.EnAttente,
            Lignes = dto.Lignes.Select(l => new LigneVente
            {
                ProduitId = l.ProduitId,
                Quantite = l.Quantite,
                PrixUnitaire = l.PrixUnitaire,
                TVA = l.TVA
            }).ToList()
        };

        vente.MontantTotalHT = vente.Lignes.Sum(l => l.Quantite * l.PrixUnitaire);
        vente.MontantTVA = vente.Lignes.Sum(l => l.Quantite * l.PrixUnitaire * (l.TVA / 100));
        vente.MontantTotal = vente.MontantTotalHT + vente.MontantTVA;

        _db.Ventes.Add(vente);
        await _db.SaveChangesAsync(ct);

        // Décrémenter stock + créer mouvements
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

        // Créer paiement initial si spécifié
        if (dto.PaiementInitial.HasValue && dto.PaiementInitial.Value > 0)
        {
            _db.Paiements.Add(new Paiement
            {
                VenteId = vente.Id,
                Montant = dto.PaiementInitial.Value,
                Methode = dto.MethodePaiementInitial ?? MethodePaiement.Espece,
                Statut = StatutPaiement.Confirme,
                DatePaiement = now
            });
            vente.MontantPaye = dto.PaiementInitial.Value;
            if (vente.MontantPaye >= vente.MontantTotal)
                vente.Statut = StatutVente.Paye;
        }

        // Générer la facture auto
        var numeroFacture = await _refGen.GenerateInvoiceReferenceAsync(ct);
        var facture = new Facture
        {
            NumeroFacture = numeroFacture,
            VenteId = vente.Id,
            DateEmission = now,
            DateEcheance = vente.DateEcheance ?? now.AddDays(30),
            Statut = vente.Statut == StatutVente.Paye ? StatutFacture.Payee : 
                     (vente.MontantPaye > 0 ? StatutFacture.PartiellementPayee : StatutFacture.EnAttente)
        };
        _db.Factures.Add(facture);

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "ventes",
            $"Vente créée : {vente.Reference} pour {client.NomClient} ({vente.MontantTotal:N2} MAD)",
            vente.Id, vente.Reference, ct: ct);

        return await GetVenteDetailsAsync(vente.Id, ct);
    }

    private async Task<VenteDto> GetVenteDetailsAsync(int id, CancellationToken ct)
    {
        var v = await _db.Ventes
            .Include(v => v.Client).Include(v => v.Utilisateur)
            .Include(v => v.Lignes).ThenInclude(l => l.Produit)
            .Include(v => v.Facture)
            .FirstAsync(v => v.Id == id, ct);
        return VenteMapper.ToDto(v);
    }
}

// ============ ANNULER VENTE ============
public record CancelVenteCommand(int Id, string? Raison) : IRequest<Unit>;

public class CancelVenteHandler : IRequestHandler<CancelVenteCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;
    private readonly IAuditLogger _audit;

    public CancelVenteHandler(IAppDbContext db, ICurrentUserService current, IAuditLogger audit)
    {
        _db = db; _current = current; _audit = audit;
    }

    public async Task<Unit> Handle(CancelVenteCommand req, CancellationToken ct)
    {
        var vente = await _db.Ventes
            .Include(v => v.Lignes)
            .Include(v => v.Facture)
            .Include(v => v.Client)
            .FirstOrDefaultAsync(v => v.Id == req.Id, ct)
            ?? throw new NotFoundException("Vente", req.Id);

        if (vente.Statut == StatutVente.Annule)
            throw new BusinessException("Cette vente est déjà annulée");

        var userId = _current.UserId ?? throw new UnauthorizedException();

        // Restaurer le stock
        foreach (var ligne in vente.Lignes)
        {
            var produit = await _db.Produits.FirstAsync(p => p.Id == ligne.ProduitId, ct);
            var stockAvant = produit.QuantiteStock;
            produit.QuantiteStock += ligne.Quantite;

            _db.MouvementsStock.Add(new MouvementStock
            {
                ProduitId = produit.Id,
                Type = TypeMouvementStock.Entree,
                Quantite = ligne.Quantite,
                StockAvant = stockAvant,
                StockApres = produit.QuantiteStock,
                Source = SourceMouvementStock.AnnulationVente,
                ReferenceId = vente.Id,
                ReferenceText = vente.Reference,
                UtilisateurId = userId,
                Raison = req.Raison ?? "Annulation vente",
                DateMouvement = DateTime.UtcNow
            });
        }

        vente.Statut = StatutVente.Annule;
        if (vente.Facture != null)
            vente.Facture.Statut = StatutFacture.Annulee;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Delete, "ventes",
            $"Vente annulée : {vente.Reference} de {vente.Client.NomClient} — Stock restauré",
            vente.Id, vente.Reference, estSensible: true, ct: ct);

        return Unit.Value;
    }
}

// ============ QUERIES ============
public record GetVentesQuery(
    int Page = 1, int PageSize = 10,
    string? Search = null,
    StatutVente? Statut = null,
    int? ClientId = null,
    DateTime? DateDebut = null,
    DateTime? DateFin = null
) : IRequest<PagedList<VenteDto>>;

public class GetVentesHandler : IRequestHandler<GetVentesQuery, PagedList<VenteDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _current;

    public GetVentesHandler(IAppDbContext db, ICurrentUserService current)
    {
        _db = db; _current = current;
    }

    public async Task<PagedList<VenteDto>> Handle(GetVentesQuery q, CancellationToken ct)
    {
        var query = _db.Ventes
            .Include(v => v.Client)
            .Include(v => v.Utilisateur)
            .Include(v => v.Lignes).ThenInclude(l => l.Produit)
            .Include(v => v.Facture)
            .AsQueryable();

        // Si client, filtrer sur ses propres ventes uniquement
        if (_current.Role == RoleUtilisateur.Client && _current.UserId.HasValue)
        {
            var clientId = await _db.Utilisateurs
                .Where(u => u.Id == _current.UserId.Value)
                .Select(u => u.ClientId)
                .FirstOrDefaultAsync(ct);
            if (clientId.HasValue) query = query.Where(v => v.ClientId == clientId);
            else query = query.Where(v => false); // Pas de client lié → rien
        }

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(v =>
                v.Reference.ToLower().Contains(s) ||
                v.Client.NomClient.ToLower().Contains(s));
        }

        if (q.Statut.HasValue) query = query.Where(v => v.Statut == q.Statut);
        if (q.ClientId.HasValue) query = query.Where(v => v.ClientId == q.ClientId);
        if (q.DateDebut.HasValue) query = query.Where(v => v.DateVente >= q.DateDebut);
        if (q.DateFin.HasValue) query = query.Where(v => v.DateVente <= q.DateFin);

        query = query.OrderByDescending(v => v.DateVente);

        var paged = await PagedList<Vente>.CreateAsync(query, q.Page, q.PageSize, ct);

        return new PagedList<VenteDto>
        {
            Items = paged.Items.Select(VenteMapper.ToDto).ToList(),
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize
        };
    }
}

public record GetVenteByIdQuery(int Id) : IRequest<VenteDto>;

public class GetVenteByIdHandler : IRequestHandler<GetVenteByIdQuery, VenteDto>
{
    private readonly IAppDbContext _db;
    public GetVenteByIdHandler(IAppDbContext db) => _db = db;

    public async Task<VenteDto> Handle(GetVenteByIdQuery q, CancellationToken ct)
    {
        var vente = await _db.Ventes
            .Include(v => v.Client).Include(v => v.Utilisateur)
            .Include(v => v.Lignes).ThenInclude(l => l.Produit)
            .Include(v => v.Facture)
            .FirstOrDefaultAsync(v => v.Id == q.Id, ct)
            ?? throw new NotFoundException("Vente", q.Id);
        return VenteMapper.ToDto(vente);
    }
}

public static class VenteMapper
{
    public static VenteDto ToDto(Vente v) => new()
    {
        Id = v.Id, Reference = v.Reference,
        ClientId = v.ClientId,
        NomClient = v.Client?.NomClient ?? "",
        ClientICE = v.Client?.ICE,
        ClientInitiales = v.Client?.Initiales,
        UtilisateurId = v.UtilisateurId,
        NomUtilisateur = v.Utilisateur?.NomComplet ?? "",
        DateVente = v.DateVente,
        DateEcheance = v.DateEcheance,
        MontantTotalHT = v.MontantTotalHT,
        MontantTVA = v.MontantTVA,
        MontantTotal = v.MontantTotal,
        MontantPaye = v.MontantPaye,
        Reste = v.Reste,
        ProgressionPaiement = v.ProgressionPaiement,
        Statut = v.Statut,
        NombreArticles = v.Lignes?.Sum(l => l.Quantite) ?? 0,
        Lignes = v.Lignes?.Select(l => new LigneVenteDto
        {
            Id = l.Id, ProduitId = l.ProduitId,
            NomProduit = l.Produit?.Nom ?? "",
            ReferenceProduit = l.Produit?.Reference ?? "",
            ImageProduit = l.Produit?.Image,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            TVA = l.TVA,
            Total = l.Total
        }).ToList() ?? new(),
        NumeroFacture = v.Facture?.NumeroFacture,
        FactureId = v.Facture?.Id
    };
}
