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
    decimal TotalResultatNet
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

        return new PLReportDto(
            annee, moisList,
            moisList.Sum(m => m.RevenuHT),
            moisList.Sum(m => m.RevenuTVA),
            moisList.Sum(m => m.RevenuTTC),
            moisList.Sum(m => m.CoutAchat),
            moisList.Sum(m => m.ChargesOp),
            moisList.Sum(m => m.ResultatBrut),
            moisList.Sum(m => m.ResultatNet)
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
