using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Reports;

// ─── DTOs ────────────────────────────────────────────────────────────────────

public record PLMoisDto(
    int    Mois,
    string NomMois,
    decimal RevenuHT,
    decimal RevenuTVA,
    decimal RevenuTTC,
    decimal CoutAchat,
    decimal ChargesOp,
    decimal ResultatBrut,
    decimal ResultatNet
);

public record PLReportDto(
    int           Annee,
    List<PLMoisDto> Mois,
    decimal TotalRevenuHT,
    decimal TotalRevenuTVA,
    decimal TotalRevenuTTC,
    decimal TotalCoutAchat,
    decimal TotalChargesOp,
    decimal TotalResultatBrut,
    decimal TotalResultatNet,
    decimal MontantImpayeCredit,
    int     NombreFacturesImpayees,
    decimal MontantImpayeDebit,
    int     NombreAchatsImpayes,
    int     NombreChargesImpayees
);

public record TVAMoisDto(
    int     Mois,
    string  NomMois,
    decimal TVACollectee,
    decimal TVADeductible,
    decimal TVANette
);

public record TVAReportDto(
    int              Annee,
    List<TVAMoisDto> Mois,
    decimal TotalTVACollectee,
    decimal TotalTVADeductible,
    decimal TotalTVANette
);

// ─── Queries ─────────────────────────────────────────────────────────────────

public record GetPLReportQuery(int Annee) : IRequest<PLReportDto>;
public record GetTVAReportQuery(int Annee) : IRequest<TVAReportDto>;

// ─── Handlers ────────────────────────────────────────────────────────────────

public class GetPLReportHandler : IRequestHandler<GetPLReportQuery, PLReportDto>
{
    private readonly IAppDbContext _db;
    public GetPLReportHandler(IAppDbContext db) => _db = db;

    public async Task<PLReportDto> Handle(GetPLReportQuery q, CancellationToken ct)
    {
        var annee = q.Annee;

        var ventes = await _db.Ventes
            .Where(v => v.DateVente.Year == annee && v.Statut != StatutVente.Annule)
            .Select(v => new { v.DateVente.Month, v.MontantTotalHT, v.MontantTVA, v.MontantTotal })
            .ToListAsync(ct);

        var lignesAchat = await _db.LignesAchat
            .Where(la => la.Achat.DateAchat.Year == annee)
            .Select(la => new { la.Achat.DateAchat.Month, Cout = la.Quantite * la.PrixUnitaire })
            .ToListAsync(ct);

        var charges = await _db.Charges
            .Where(c => c.DateCharge.Year == annee)
            .Select(c => new { c.DateCharge.Month, c.Montant })
            .ToListAsync(ct);

        var noms = new[] { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                           "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };

        var moisList = Enumerable.Range(1, 12).Select(m =>
        {
            var revHT  = ventes.Where(v => v.Month == m).Sum(v => v.MontantTotalHT);
            var revTVA = ventes.Where(v => v.Month == m).Sum(v => v.MontantTVA);
            var revTTC = ventes.Where(v => v.Month == m).Sum(v => v.MontantTotal);
            var cout   = lignesAchat.Where(la => la.Month == m).Sum(la => la.Cout);
            var op     = charges.Where(c => c.Month == m).Sum(c => c.Montant);
            var brut   = revHT - cout;
            var net    = brut - op;
            return new PLMoisDto(m, noms[m], revHT, revTVA, revTTC, cout, op, brut, net);
        }).ToList();

        var impayeCredit = await _db.Ventes
            .Where(v => v.Statut == StatutVente.EnAttente)
            .SumAsync(v => (decimal?)(v.MontantTotal - v.MontantPaye) ?? 0, ct);
        var nbFacturesImpayees = await _db.Factures
            .CountAsync(f => f.Statut == StatutFacture.EnAttente || f.Statut == StatutFacture.EnRetard, ct);
        var impayeAchats = await _db.Achats
            .Where(a => a.Statut == StatutAchat.EnAttente || a.Statut == StatutAchat.Partiel)
            .SumAsync(a => (decimal?)(a.MontantTotal - a.MontantPaye) ?? 0, ct);
        var impayeCharges = await _db.Charges
            .Where(c => c.Statut == StatutCharge.EnAttente || c.Statut == StatutCharge.Partiel)
            .SumAsync(c => (decimal?)(c.Montant - c.MontantPaye) ?? 0, ct);
        var nbAchatsImpay  = await _db.Achats.CountAsync(a => a.Statut == StatutAchat.EnAttente || a.Statut == StatutAchat.Partiel, ct);
        var nbChargesImpay = await _db.Charges.CountAsync(c => c.Statut == StatutCharge.EnAttente || c.Statut == StatutCharge.Partiel, ct);

        return new PLReportDto(
            annee, moisList,
            moisList.Sum(m => m.RevenuHT),
            moisList.Sum(m => m.RevenuTVA),
            moisList.Sum(m => m.RevenuTTC),
            moisList.Sum(m => m.CoutAchat),
            moisList.Sum(m => m.ChargesOp),
            moisList.Sum(m => m.ResultatBrut),
            moisList.Sum(m => m.ResultatNet),
            impayeCredit,
            nbFacturesImpayees,
            impayeAchats + impayeCharges,
            nbAchatsImpay,
            nbChargesImpay
        );
    }
}

public class GetTVAReportHandler : IRequestHandler<GetTVAReportQuery, TVAReportDto>
{
    private readonly IAppDbContext _db;
    public GetTVAReportHandler(IAppDbContext db) => _db = db;

    public async Task<TVAReportDto> Handle(GetTVAReportQuery q, CancellationToken ct)
    {
        var annee = q.Annee;

        var tvaVentes = await _db.Ventes
            .Where(v => v.DateVente.Year == annee && v.Statut != StatutVente.Annule)
            .Select(v => new { v.DateVente.Month, v.MontantTVA })
            .ToListAsync(ct);

        // TVA déductible calculée depuis les lignes achat × taux TVA du produit
        var tvaAchats = await _db.LignesAchat
            .Where(la => la.Achat.DateAchat.Year == annee)
            .Select(la => new
            {
                la.Achat.DateAchat.Month,
                TVA = la.Quantite * la.PrixUnitaire * la.Produit.TVA / 100m
            })
            .ToListAsync(ct);

        var noms = new[] { "", "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
                           "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre" };

        var moisList = Enumerable.Range(1, 12).Select(m =>
        {
            var collectee  = tvaVentes.Where(v => v.Month == m).Sum(v => v.MontantTVA);
            var deductible = tvaAchats.Where(a => a.Month == m).Sum(a => a.TVA);
            return new TVAMoisDto(m, noms[m], collectee, deductible, collectee - deductible);
        }).ToList();

        return new TVAReportDto(
            annee, moisList,
            moisList.Sum(m => m.TVACollectee),
            moisList.Sum(m => m.TVADeductible),
            moisList.Sum(m => m.TVANette)
        );
    }
}

// ─── Cash Flow ───────────────────────────────────────────────────────────────

public record CashFlowJourDto(string Date, decimal Entrees, decimal Sorties, decimal Net);

public record CashFlowReportDto(
    int Annee, int Mois,
    List<CashFlowJourDto> Jours,
    decimal TotalEntrees,
    decimal TotalSorties,
    decimal NetMois
);

public record GetCashFlowQuery(int Annee, int Mois) : IRequest<CashFlowReportDto>;

public class GetCashFlowHandler : IRequestHandler<GetCashFlowQuery, CashFlowReportDto>
{
    private readonly IAppDbContext _db;
    public GetCashFlowHandler(IAppDbContext db) => _db = db;

    public async Task<CashFlowReportDto> Handle(GetCashFlowQuery q, CancellationToken ct)
    {
        var debut = new DateTime(q.Annee, q.Mois, 1);
        var fin   = debut.AddMonths(1);

        // Encaissements : paiements reçus des clients (ventes)
        var entrees = await _db.Paiements
            .Where(p => p.DatePaiement >= debut && p.DatePaiement < fin && p.Statut != StatutPaiement.Annule)
            .Select(p => new { Date = p.DatePaiement.Date, p.Montant })
            .ToListAsync(ct);

        // Décaissements : paiements achats + paiements charges
        var sortiesAchats = await _db.PaiementsAchat
            .Where(p => p.DatePaiement >= debut && p.DatePaiement < fin)
            .Select(p => new { Date = p.DatePaiement.Date, p.Montant })
            .ToListAsync(ct);

        var sortiesCharges = await _db.PaiementsCharge
            .Where(p => p.DatePaiement >= debut && p.DatePaiement < fin)
            .Select(p => new { Date = p.DatePaiement.Date, p.Montant })
            .ToListAsync(ct);

        var jours = Enumerable.Range(1, DateTime.DaysInMonth(q.Annee, q.Mois)).Select(d =>
        {
            var date = new DateTime(q.Annee, q.Mois, d).Date;
            var e = entrees.Where(p => p.Date == date).Sum(p => p.Montant);
            var s = sortiesAchats.Where(p => p.Date == date).Sum(p => p.Montant)
                  + sortiesCharges.Where(p => p.Date == date).Sum(p => p.Montant);
            return new CashFlowJourDto(date.ToString("dd/MM"), e, s, e - s);
        }).ToList();

        return new CashFlowReportDto(
            q.Annee, q.Mois, jours,
            jours.Sum(j => j.Entrees),
            jours.Sum(j => j.Sorties),
            jours.Sum(j => j.Net)
        );
    }
}

// ─── Balance Âgée (Receivables Aging) ────────────────────────────────────────

public record BalanceAgeeClientDto(
    int     ClientId,
    string  NomClient,
    string  Initiales,
    decimal TotalImpaye,
    decimal Courant,
    decimal J1_30,
    decimal J31_60,
    decimal J61_90,
    decimal J90Plus
);

public record BalanceAgeeReportDto(
    DateTime                   DateArrete,
    List<BalanceAgeeClientDto> Clients,
    decimal TotalImpaye,
    decimal TotalCourant,
    decimal TotalJ1_30,
    decimal TotalJ31_60,
    decimal TotalJ61_90,
    decimal TotalJ90Plus,
    int     NombreClients
);

public record GetBalanceAgeeQuery() : IRequest<BalanceAgeeReportDto>;

public class GetBalanceAgeeHandler : IRequestHandler<GetBalanceAgeeQuery, BalanceAgeeReportDto>
{
    private readonly IAppDbContext _db;
    public GetBalanceAgeeHandler(IAppDbContext db) => _db = db;

    public async Task<BalanceAgeeReportDto> Handle(GetBalanceAgeeQuery q, CancellationToken ct)
    {
        var today = DateTime.Today;

        var ventesImpayees = await _db.Ventes
            .Where(v => v.Statut != StatutVente.Annule && v.MontantPaye < v.MontantTotal)
            .Select(v => new {
                v.ClientId,
                v.Client.NomClient,
                v.MontantTotal,
                v.MontantPaye,
                v.DateEcheance,
                v.DateVente
            })
            .ToListAsync(ct);

        var byClient = ventesImpayees
            .GroupBy(v => new { v.ClientId, v.NomClient })
            .Select(g =>
            {
                decimal courant = 0, j1_30 = 0, j31_60 = 0, j61_90 = 0, j90plus = 0;
                foreach (var v in g)
                {
                    var reste    = v.MontantTotal - v.MontantPaye;
                    var echeance = (v.DateEcheance ?? v.DateVente.AddDays(30)).Date;
                    var jours    = (today - echeance).Days;
                    if      (jours <= 0)  courant += reste;
                    else if (jours <= 30) j1_30   += reste;
                    else if (jours <= 60) j31_60  += reste;
                    else if (jours <= 90) j61_90  += reste;
                    else                  j90plus += reste;
                }
                var nom   = g.Key.NomClient;
                var parts = nom.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var init  = parts.Length >= 2
                    ? (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant()
                    : nom.Length >= 2 ? nom[..2].ToUpperInvariant() : nom.ToUpperInvariant();

                return new BalanceAgeeClientDto(
                    g.Key.ClientId, nom, init,
                    courant + j1_30 + j31_60 + j61_90 + j90plus,
                    courant, j1_30, j31_60, j61_90, j90plus
                );
            })
            .OrderByDescending(c => c.TotalImpaye)
            .ToList();

        return new BalanceAgeeReportDto(
            today, byClient,
            byClient.Sum(c => c.TotalImpaye),
            byClient.Sum(c => c.Courant),
            byClient.Sum(c => c.J1_30),
            byClient.Sum(c => c.J31_60),
            byClient.Sum(c => c.J61_90),
            byClient.Sum(c => c.J90Plus),
            byClient.Count
        );
    }
}

// ─── Performance Commerciale ─────────────────────────────────────────────────

public record TopClientPerfDto(
    int     ClientId,
    string  NomClient,
    string  Initiales,
    int     NombreVentes,
    decimal MontantTotalHT,
    decimal MontantTotal,
    decimal MontantPaye,
    decimal MontantImpaye,
    decimal PanierMoyen
);

public record TopProduitPerfDto(
    int     ProduitId,
    string  NomProduit,
    string  Reference,
    int     QuantiteVendue,
    decimal MontantHT,
    decimal PourcentageCA
);

public record PerformanceCommercialeDto(
    int                     Annee,
    int                     NombreVentes,
    int                     NombreClients,
    decimal                 CAHt,
    decimal                 CATtc,
    List<TopClientPerfDto>  TopClients,
    List<TopProduitPerfDto> TopProduits
);

public record GetPerformanceCommercialeQuery(int Annee) : IRequest<PerformanceCommercialeDto>;

public class GetPerformanceCommercialeHandler : IRequestHandler<GetPerformanceCommercialeQuery, PerformanceCommercialeDto>
{
    private readonly IAppDbContext _db;
    public GetPerformanceCommercialeHandler(IAppDbContext db) => _db = db;

    public async Task<PerformanceCommercialeDto> Handle(GetPerformanceCommercialeQuery q, CancellationToken ct)
    {
        var ventes = await _db.Ventes
            .Where(v => v.DateVente.Year == q.Annee && v.Statut != StatutVente.Annule)
            .Select(v => new {
                v.ClientId,
                v.Client.NomClient,
                v.MontantTotalHT,
                v.MontantTotal,
                v.MontantPaye,
                Reste = v.MontantTotal - v.MontantPaye
            })
            .ToListAsync(ct);

        var lignes = await _db.LignesVente
            .Where(lv => lv.Vente.DateVente.Year == q.Annee && lv.Vente.Statut != StatutVente.Annule)
            .Select(lv => new {
                lv.ProduitId,
                NomProduit = lv.Produit.Nom,
                Reference  = lv.Produit.Reference,
                lv.Quantite,
                MontantHT  = lv.Quantite * lv.PrixUnitaire
            })
            .ToListAsync(ct);

        var topClients = ventes
            .GroupBy(v => new { v.ClientId, v.NomClient })
            .Select(g =>
            {
                var nom   = g.Key.NomClient;
                var parts = nom.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var init  = parts.Length >= 2
                    ? (parts[0][0].ToString() + parts[1][0].ToString()).ToUpperInvariant()
                    : nom.Length >= 2 ? nom[..2].ToUpperInvariant() : nom.ToUpperInvariant();
                var count = g.Count();
                var ttl   = g.Sum(v => v.MontantTotal);
                return new TopClientPerfDto(
                    g.Key.ClientId, nom, init,
                    count,
                    g.Sum(v => v.MontantTotalHT),
                    ttl,
                    g.Sum(v => v.MontantPaye),
                    g.Sum(v => v.Reste),
                    count > 0 ? Math.Round(ttl / count, 2) : 0
                );
            })
            .OrderByDescending(c => c.MontantTotal)
            .Take(10)
            .ToList();

        var totalCA = lignes.Sum(l => l.MontantHT);

        var topProduits = lignes
            .GroupBy(l => new { l.ProduitId, l.NomProduit, l.Reference })
            .Select(g =>
            {
                var montant = g.Sum(l => l.MontantHT);
                return new TopProduitPerfDto(
                    g.Key.ProduitId,
                    g.Key.NomProduit,
                    g.Key.Reference,
                    g.Sum(l => l.Quantite),
                    montant,
                    totalCA > 0 ? Math.Round(montant / totalCA * 100, 1) : 0
                );
            })
            .OrderByDescending(p => p.MontantHT)
            .Take(10)
            .ToList();

        return new PerformanceCommercialeDto(
            q.Annee,
            ventes.Count,
            ventes.Select(v => v.ClientId).Distinct().Count(),
            ventes.Sum(v => v.MontantTotalHT),
            ventes.Sum(v => v.MontantTotal),
            topClients,
            topProduits
        );
    }
}
