using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Settings;

public class FacturationSettingsDto
{
    public string PrefixeFacture { get; set; } = "FAC";
    public string PrefixeVente { get; set; } = "VNT";
    public string PrefixeAchat { get; set; } = "ACH";
    public string PrefixeProduit { get; set; } = "PRD";
    public int TvaParDefaut { get; set; } = 20;
    public int DelaiPaiement { get; set; } = 30;
}

public record GetFacturationSettingsQuery() : IRequest<FacturationSettingsDto>;
public record UpdateFacturationSettingsCommand(FacturationSettingsDto Dto) : IRequest<FacturationSettingsDto>;

public class GetFacturationSettingsHandler : IRequestHandler<GetFacturationSettingsQuery, FacturationSettingsDto>
{
    private readonly IAppDbContext _db;
    public GetFacturationSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<FacturationSettingsDto> Handle(GetFacturationSettingsQuery request, CancellationToken ct)
    {
        var pf = await _db.ParametresFacturation.AsNoTracking().FirstOrDefaultAsync(ct);
        if (pf == null) return new FacturationSettingsDto();
        return new FacturationSettingsDto
        {
            PrefixeFacture = pf.PrefixeFacture,
            PrefixeVente = pf.PrefixeVente,
            PrefixeAchat = pf.PrefixeAchat,
            PrefixeProduit = pf.PrefixeProduit,
            TvaParDefaut = pf.TvaParDefaut,
            DelaiPaiement = pf.DelaiPaiement,
        };
    }
}

public class UpdateFacturationSettingsHandler : IRequestHandler<UpdateFacturationSettingsCommand, FacturationSettingsDto>
{
    private readonly IAppDbContext _db;
    public UpdateFacturationSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<FacturationSettingsDto> Handle(UpdateFacturationSettingsCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var pf = await _db.ParametresFacturation.FirstOrDefaultAsync(ct);
        bool isNew = pf == null;
        if (pf == null)
        {
            pf = new ParametresFacturation();
            _db.ParametresFacturation.Add(pf);
        }

        var oldVente   = pf.PrefixeVente;
        var oldFacture = pf.PrefixeFacture;
        var oldAchat   = pf.PrefixeAchat;
        var oldProduit = pf.PrefixeProduit;

        var newVente   = (dto.PrefixeVente   ?? "VNT").Trim().ToUpperInvariant();
        var newFacture = (dto.PrefixeFacture  ?? "FAC").Trim().ToUpperInvariant();
        var newAchat   = (dto.PrefixeAchat    ?? "ACH").Trim().ToUpperInvariant();
        var newProduit = (dto.PrefixeProduit  ?? "PRD").Trim().ToUpperInvariant();

        pf.PrefixeFacture = newFacture;
        pf.PrefixeVente   = newVente;
        pf.PrefixeAchat   = newAchat;
        pf.PrefixeProduit = newProduit;
        pf.TvaParDefaut   = dto.TvaParDefaut;
        pf.DelaiPaiement  = dto.DelaiPaiement;
        pf.UpdatedAt      = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        if (!isNew)
        {
            if (oldVente != newVente && !string.IsNullOrEmpty(oldVente))
                await _db.Ventes
                    .Where(v => v.Reference.StartsWith(oldVente + "-"))
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        v => v.Reference,
                        v => newVente + v.Reference.Substring(oldVente.Length)), ct);

            if (oldFacture != newFacture && !string.IsNullOrEmpty(oldFacture))
                await _db.Factures
                    .Where(f => f.NumeroFacture.StartsWith(oldFacture + "-"))
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        f => f.NumeroFacture,
                        f => newFacture + f.NumeroFacture.Substring(oldFacture.Length)), ct);

            if (oldAchat != newAchat && !string.IsNullOrEmpty(oldAchat))
                await _db.Achats
                    .Where(a => a.Reference.StartsWith(oldAchat + "-"))
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        a => a.Reference,
                        a => newAchat + a.Reference.Substring(oldAchat.Length)), ct);

            if (oldProduit != newProduit && !string.IsNullOrEmpty(oldProduit))
                await _db.Produits
                    .Where(p => p.Reference.StartsWith(oldProduit + "-"))
                    .ExecuteUpdateAsync(s => s.SetProperty(
                        p => p.Reference,
                        p => newProduit + p.Reference.Substring(oldProduit.Length)), ct);
        }

        return new FacturationSettingsDto
        {
            PrefixeFacture = pf.PrefixeFacture,
            PrefixeVente   = pf.PrefixeVente,
            PrefixeAchat   = pf.PrefixeAchat,
            PrefixeProduit = pf.PrefixeProduit,
            TvaParDefaut   = pf.TvaParDefaut,
            DelaiPaiement  = pf.DelaiPaiement,
        };
    }
}
