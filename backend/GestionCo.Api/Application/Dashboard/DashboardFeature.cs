using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Dashboard;

public class DashboardStatsDto
{
    public decimal CaDuMois { get; set; }
    public decimal CaDuJour { get; set; }
    public int VentesDuMois { get; set; }
    public int VentesDuJour { get; set; }
    public int TotalClients { get; set; }
    public int NouveauxClientsDuMois { get; set; }
    public int TotalProduits { get; set; }
    public int ProduitsStockFaible { get; set; }
    public int ProduitsRupture { get; set; }
    public decimal MontantImpaye { get; set; }
    public int NombreFacturesImpayees { get; set; }
}

public class TopProduitDto
{
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int QuantiteVendue { get; set; }
    public decimal MontantTotal { get; set; }
}

public class VenteParJourDto
{
    public DateTime Date { get; set; }
    public decimal Montant { get; set; }
    public int NombreVentes { get; set; }
}

public class StockAlerteDto
{
    public int ProduitId { get; set; }
    public string NomProduit { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int QuantiteStock { get; set; }
    public int SeuilAlerte { get; set; }
    public bool EstRupture { get; set; }
}

public class DernieresVentesDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public decimal MontantTotal { get; set; }
    public DateTime DateVente { get; set; }
    public StatutVente Statut { get; set; }
}

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;
public record GetDashboardChartQuery(int Jours = 30) : IRequest<List<VenteParJourDto>>;
public record GetTopProduitsQuery(int Take = 5) : IRequest<List<TopProduitDto>>;
public record GetStockAlertesQuery(int Take = 10) : IRequest<List<StockAlerteDto>>;
public record GetDernieresVentesQuery(int Take = 10) : IRequest<List<DernieresVentesDto>>;

public class DashboardHandlers :
    IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>,
    IRequestHandler<GetDashboardChartQuery, List<VenteParJourDto>>,
    IRequestHandler<GetTopProduitsQuery, List<TopProduitDto>>,
    IRequestHandler<GetStockAlertesQuery, List<StockAlerteDto>>,
    IRequestHandler<GetDernieresVentesQuery, List<DernieresVentesDto>>
{
    private readonly IAppDbContext _db;
    public DashboardHandlers(IAppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery q, CancellationToken ct)
    {
        var maintenant = DateTime.UtcNow;
        var debutMois = new DateTime(maintenant.Year, maintenant.Month, 1);
        var debutJour = maintenant.Date;

        return new DashboardStatsDto
        {
            CaDuMois = await _db.Ventes
                .Where(v => v.DateVente >= debutMois && v.Statut != StatutVente.Annule)
                .SumAsync(v => v.MontantTotal, ct),
            CaDuJour = await _db.Ventes
                .Where(v => v.DateVente >= debutJour && v.Statut != StatutVente.Annule)
                .SumAsync(v => v.MontantTotal, ct),
            VentesDuMois = await _db.Ventes
                .Where(v => v.DateVente >= debutMois && v.Statut != StatutVente.Annule)
                .CountAsync(ct),
            VentesDuJour = await _db.Ventes
                .Where(v => v.DateVente >= debutJour && v.Statut != StatutVente.Annule)
                .CountAsync(ct),
            TotalClients = await _db.Clients.CountAsync(c => c.IsActive, ct),
            NouveauxClientsDuMois = await _db.Clients
                .Where(c => c.CreatedAt >= debutMois).CountAsync(ct),
            TotalProduits = await _db.Produits.CountAsync(p => p.IsActive, ct),
            ProduitsStockFaible = await _db.Produits
                .Where(p => p.QuantiteStock > 0 && p.QuantiteStock <= p.SeuilAlerte)
                .CountAsync(ct),
            ProduitsRupture = await _db.Produits.CountAsync(p => p.QuantiteStock == 0, ct),
            MontantImpaye = await _db.Ventes
                .Where(v => v.Statut == StatutVente.EnAttente)
                .SumAsync(v => v.MontantTotal - v.MontantPaye, ct),
            NombreFacturesImpayees = await _db.Factures
                .Where(f => f.Statut == StatutFacture.EnAttente || f.Statut == StatutFacture.EnRetard)
                .CountAsync(ct)
        };
    }

    public async Task<List<VenteParJourDto>> Handle(GetDashboardChartQuery q, CancellationToken ct)
    {
        var dateDebut = DateTime.UtcNow.Date.AddDays(-q.Jours);
        var ventes = await _db.Ventes
            .Where(v => v.DateVente >= dateDebut && v.Statut != StatutVente.Annule)
            .GroupBy(v => v.DateVente.Date)
            .Select(g => new VenteParJourDto
            {
                Date = g.Key,
                Montant = g.Sum(v => v.MontantTotal),
                NombreVentes = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);
        return ventes;
    }

    public async Task<List<TopProduitDto>> Handle(GetTopProduitsQuery q, CancellationToken ct)
    {
        return await _db.LignesVente
            .Include(l => l.Produit)
            .Include(l => l.Vente)
            .Where(l => l.Vente.Statut != StatutVente.Annule)
            .GroupBy(l => new { l.ProduitId, l.Produit.Nom, l.Produit.Image })
            .Select(g => new TopProduitDto
            {
                ProduitId = g.Key.ProduitId,
                NomProduit = g.Key.Nom,
                Image = g.Key.Image,
                QuantiteVendue = g.Sum(x => x.Quantite),
                MontantTotal = g.Sum(x => x.Quantite * x.PrixUnitaire)
            })
            .OrderByDescending(x => x.QuantiteVendue)
            .Take(q.Take)
            .ToListAsync(ct);
    }

    public async Task<List<StockAlerteDto>> Handle(GetStockAlertesQuery q, CancellationToken ct)
    {
        return await _db.Produits
            .Where(p => p.IsActive && p.QuantiteStock <= p.SeuilAlerte)
            .OrderBy(p => p.QuantiteStock)
            .Take(q.Take)
            .Select(p => new StockAlerteDto
            {
                ProduitId = p.Id, NomProduit = p.Nom,
                Reference = p.Reference, Image = p.Image,
                QuantiteStock = p.QuantiteStock, SeuilAlerte = p.SeuilAlerte,
                EstRupture = p.QuantiteStock == 0
            })
            .ToListAsync(ct);
    }

    public async Task<List<DernieresVentesDto>> Handle(GetDernieresVentesQuery q, CancellationToken ct)
    {
        return await _db.Ventes
            .Include(v => v.Client)
            .OrderByDescending(v => v.DateVente)
            .Take(q.Take)
            .Select(v => new DernieresVentesDto
            {
                Id = v.Id, Reference = v.Reference,
                NomClient = v.Client.NomClient,
                MontantTotal = v.MontantTotal,
                DateVente = v.DateVente, Statut = v.Statut
            })
            .ToListAsync(ct);
    }
}
