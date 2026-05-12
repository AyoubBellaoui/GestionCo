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
public record GetFullDashboardQuery : IRequest<FullDashboardDto>;

// ─── Full dashboard DTO ───────────────────────────────────────────────────────
public class MonthBarDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Ca { get; set; }
    public decimal Dep { get; set; }
}

public class DonutItemDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class TopClientDashDto
{
    public int ClientId { get; set; }
    public string NomClient { get; set; } = string.Empty;
    public string Initiales { get; set; } = string.Empty;
    public decimal TotalDepense { get; set; }
}

public class FullDashboardDto
{
    // Basic counts
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
    public decimal MontantImpayeDebit { get; set; }
    public int NombreAchatsImpayes { get; set; }
    public int NombreChargesImpayees { get; set; }

    // Financial metrics
    public decimal AchatsDuMois { get; set; }
    public decimal ChargesDuMois { get; set; }
    public decimal DepensesDuMois { get; set; }
    public decimal ResultatNet { get; set; }
    public int MargeRate { get; set; }

    // Trends (% vs last month)
    public int TrendRevenu { get; set; }
    public int TrendDepenses { get; set; }

    // Rates
    public int TauxEncaissement { get; set; }
    public int TauxFidelite { get; set; }
    public decimal PanierMoyen { get; set; }
    public int TotalQteVendue { get; set; }

    // Charts
    public List<MonthBarDto> Last6Months { get; set; } = [];
    public List<DonutItemDto> DonutItems { get; set; } = [];

    // Lists
    public List<TopClientDashDto> TopClients { get; set; } = [];
    public List<TopProduitDto> TopProduits { get; set; } = [];
    public List<StockAlerteDto> StockAlertes { get; set; } = [];
}

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

public class GetFullDashboardHandler : IRequestHandler<GetFullDashboardQuery, FullDashboardDto>
{
    private readonly IAppDbContext _db;
    public GetFullDashboardHandler(IAppDbContext db) => _db = db;

    public async Task<FullDashboardDto> Handle(GetFullDashboardQuery _, CancellationToken ct)
    {
        var now      = DateTime.UtcNow;
        var debutM   = new DateTime(now.Year, now.Month, 1);
        var debutJ   = now.Date;
        var debutLM  = debutM.AddMonths(-1);
        var debut6M  = debutM.AddMonths(-5);

        // ── Parallel DB queries ──────────────────────────────────────────────
        var caM    = await _db.Ventes.Where(v => v.DateVente >= debutM  && v.Statut != StatutVente.Annule).SumAsync(v => (decimal?)v.MontantTotal ?? 0, ct);
        var caJ    = await _db.Ventes.Where(v => v.DateVente >= debutJ  && v.Statut != StatutVente.Annule).SumAsync(v => (decimal?)v.MontantTotal ?? 0, ct);
        var caLM   = await _db.Ventes.Where(v => v.DateVente >= debutLM && v.DateVente < debutM && v.Statut != StatutVente.Annule).SumAsync(v => (decimal?)v.MontantTotal ?? 0, ct);
        var nbVM   = await _db.Ventes.CountAsync(v => v.DateVente >= debutM  && v.Statut != StatutVente.Annule, ct);
        var nbVJ   = await _db.Ventes.CountAsync(v => v.DateVente >= debutJ  && v.Statut != StatutVente.Annule, ct);

        var achM   = await _db.LignesAchat.Where(la => la.Achat.DateAchat >= debutM ).SumAsync(la => (decimal?)(la.Quantite * la.PrixUnitaire) ?? 0, ct);
        var achLM  = await _db.LignesAchat.Where(la => la.Achat.DateAchat >= debutLM && la.Achat.DateAchat < debutM).SumAsync(la => (decimal?)(la.Quantite * la.PrixUnitaire) ?? 0, ct);
        var chgM   = await _db.Charges.Where(c => c.DateCharge >= debutM ).SumAsync(c => (decimal?)c.Montant ?? 0, ct);
        var chgLM  = await _db.Charges.Where(c => c.DateCharge >= debutLM && c.DateCharge < debutM).SumAsync(c => (decimal?)c.Montant ?? 0, ct);

        var depM  = achM  + chgM;
        var depLM = achLM + chgLM;
        var res   = caM - depM;
        var marge = caM > 0 ? (int)(res / caM * 100) : 0;

        var trendRev = caLM  > 0 ? (int)((caM  - caLM)  / caLM  * 100) : 0;
        var trendDep = depLM > 0 ? (int)((depM - depLM) / depLM * 100) : 0;

        // Taux encaissement
        var totalV  = await _db.Ventes.Where(v => v.Statut != StatutVente.Annule).SumAsync(v => (decimal?)v.MontantTotal ?? 0, ct);
        var totalP  = await _db.Ventes.Where(v => v.Statut != StatutVente.Annule).SumAsync(v => (decimal?)v.MontantPaye  ?? 0, ct);
        var tauxEnc = totalV > 0 ? (int)(totalP / totalV * 100) : 0;

        // Clients
        var nbClients   = await _db.Clients.CountAsync(c => c.IsActive, ct);
        var newClients  = await _db.Clients.CountAsync(c => c.CreatedAt >= debutM, ct);
        var totalProds  = await _db.Produits.CountAsync(p => p.IsActive, ct);
        var stockFaible = await _db.Produits.CountAsync(p => p.QuantiteStock > 0 && p.QuantiteStock <= p.SeuilAlerte, ct);
        var rupture     = await _db.Produits.CountAsync(p => p.QuantiteStock == 0, ct);
        var impaye      = await _db.Ventes.Where(v => v.Statut == StatutVente.EnAttente).SumAsync(v => (decimal?)(v.MontantTotal - v.MontantPaye) ?? 0, ct);
        var nbImpaye    = await _db.Factures.CountAsync(f => f.Statut == StatutFacture.EnAttente || f.Statut == StatutFacture.EnRetard, ct);

        var impayeAchats  = await _db.Achats
            .Where(a => a.Statut == StatutAchat.EnAttente || a.Statut == StatutAchat.Partiel)
            .Select(a => a.MontantTotal - a.MontantPaye)
            .SumAsync(ct);
        var impayeCharges = await _db.Charges
            .Where(c => c.Statut == StatutCharge.EnAttente || c.Statut == StatutCharge.Partiel)
            .Select(c => c.Montant - c.MontantPaye)
            .SumAsync(ct);
        var nbAchatsImpay  = await _db.Achats.CountAsync(a => a.Statut == StatutAchat.EnAttente || a.Statut == StatutAchat.Partiel, ct);
        var nbChargesImpay = await _db.Charges.CountAsync(c => c.Statut == StatutCharge.EnAttente || c.Statut == StatutCharge.Partiel, ct);

        // Taux fidélité — clients who bought more than once
        var ordersPerClient = await _db.Ventes
            .Where(v => v.Statut != StatutVente.Annule)
            .GroupBy(v => v.ClientId)
            .Select(g => new { Count = g.Count() })
            .ToListAsync(ct);
        var tauxFid = ordersPerClient.Count > 0 ? (int)((double)ordersPerClient.Count(c => c.Count > 1) / ordersPerClient.Count * 100) : 0;

        // Panier moyen + total qty
        var panierMoyen = nbVM > 0 ? caM / nbVM : 0;
        var totalQte = await _db.LignesVente.Where(l => l.Vente.Statut != StatutVente.Annule).SumAsync(l => (int?)l.Quantite ?? 0, ct);

        // Last 6 months bars (CA + Depenses)
        var ventesBy6M = await _db.Ventes
            .Where(v => v.DateVente >= debut6M && v.Statut != StatutVente.Annule)
            .GroupBy(v => new { v.DateVente.Year, v.DateVente.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Ca = g.Sum(v => v.MontantTotal) })
            .ToListAsync(ct);
        var achatBy6M = await _db.LignesAchat
            .Where(la => la.Achat.DateAchat >= debut6M)
            .GroupBy(la => new { la.Achat.DateAchat.Year, la.Achat.DateAchat.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(la => la.Quantite * la.PrixUnitaire) })
            .ToListAsync(ct);
        var chargeBy6M = await _db.Charges
            .Where(c => c.DateCharge >= debut6M)
            .GroupBy(c => new { c.DateCharge.Year, c.DateCharge.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(c => c.Montant) })
            .ToListAsync(ct);

        var bars = Enumerable.Range(0, 6).Select(i =>
        {
            var d  = new DateTime(now.Year, now.Month, 1).AddMonths(i - 5);
            var ca = ventesBy6M .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Ca    ?? 0;
            var ac = achatBy6M  .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Total ?? 0;
            var ch = chargeBy6M .FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month)?.Total ?? 0;
            return new MonthBarDto { Label = d.ToString("MMM", new System.Globalization.CultureInfo("fr-FR")), Ca = ca, Dep = ac + ch };
        }).ToList();

        // Donut
        var donut = new List<DonutItemDto>
        {
            new() { Label = "Ventes",  Amount = caM,  Color = "#4F46E5" },
            new() { Label = "Achats",  Amount = achM, Color = "#ef4444" },
            new() { Label = "Charges", Amount = chgM, Color = "#f59e0b" },
        };

        // Top clients — Initiales is a computed C# property, compute in memory after query
        var topClients = (await _db.Ventes
            .Where(v => v.Statut != StatutVente.Annule)
            .GroupBy(v => new { v.ClientId, v.Client.NomClient })
            .Select(g => new { g.Key.ClientId, g.Key.NomClient, TotalDepense = g.Sum(v => v.MontantTotal) })
            .OrderByDescending(x => x.TotalDepense)
            .Take(5)
            .ToListAsync(ct))
            .Select(x =>
            {
                var parts = x.NomClient.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var initiales = parts.Length >= 2
                    ? $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant()
                    : x.NomClient.Length >= 2 ? x.NomClient[..2].ToUpperInvariant() : x.NomClient.ToUpperInvariant();
                return new TopClientDashDto { ClientId = x.ClientId, NomClient = x.NomClient, Initiales = initiales, TotalDepense = x.TotalDepense };
            })
            .ToList();

        // Top produits
        var topProduits = await _db.LignesVente
            .Where(l => l.Vente.Statut != StatutVente.Annule)
            .GroupBy(l => new { l.ProduitId, l.Produit.Nom, l.Produit.Image })
            .Select(g => new TopProduitDto
            {
                ProduitId      = g.Key.ProduitId,
                NomProduit     = g.Key.Nom,
                Image          = g.Key.Image,
                QuantiteVendue = g.Sum(x => x.Quantite),
                MontantTotal   = g.Sum(x => x.Quantite * x.PrixUnitaire)
            })
            .OrderByDescending(x => x.MontantTotal)
            .Take(5)
            .ToListAsync(ct);

        // Stock alertes
        var alertes = await _db.Produits
            .Where(p => p.IsActive && p.QuantiteStock <= p.SeuilAlerte)
            .OrderBy(p => p.QuantiteStock).Take(10)
            .Select(p => new StockAlerteDto
            {
                ProduitId = p.Id, NomProduit = p.Nom, Reference = p.Reference, Image = p.Image,
                QuantiteStock = p.QuantiteStock, SeuilAlerte = p.SeuilAlerte, EstRupture = p.QuantiteStock == 0
            })
            .ToListAsync(ct);

        return new FullDashboardDto
        {
            CaDuMois = caM, CaDuJour = caJ, VentesDuMois = nbVM, VentesDuJour = nbVJ,
            TotalClients = nbClients, NouveauxClientsDuMois = newClients,
            TotalProduits = totalProds, ProduitsStockFaible = stockFaible, ProduitsRupture = rupture,
            MontantImpaye = impaye, NombreFacturesImpayees = nbImpaye,
            MontantImpayeDebit = impayeAchats + impayeCharges,
            NombreAchatsImpayes = nbAchatsImpay, NombreChargesImpayees = nbChargesImpay,
            AchatsDuMois = achM, ChargesDuMois = chgM, DepensesDuMois = depM,
            ResultatNet = res, MargeRate = marge,
            TrendRevenu = trendRev, TrendDepenses = trendDep,
            TauxEncaissement = tauxEnc, TauxFidelite = tauxFid,
            PanierMoyen = panierMoyen, TotalQteVendue = totalQte,
            Last6Months = bars, DonutItems = donut,
            TopClients = topClients, TopProduits = topProduits, StockAlertes = alertes,
        };
    }
}
