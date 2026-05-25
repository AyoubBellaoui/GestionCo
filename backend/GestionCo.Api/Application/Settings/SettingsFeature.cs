using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ParametresEntrepriseEntity = GestionCo.Api.Domain.Entities.ParametresEntreprise;

namespace GestionCo.Api.Application.Settings;

public class FacturationSettingsDto
{
    public string PrefixeFacture { get; set; } = "FAC";
    public string PrefixeVente { get; set; } = "VNT";
    public string PrefixeAchat { get; set; } = "ACH";
    public string PrefixeProduit { get; set; } = "PRD";
    public int TvaParDefaut { get; set; } = 20;
    public int DelaiPaiement { get; set; } = 30;
    public bool IncludeAnnee { get; set; } = true;
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
            IncludeAnnee = pf.IncludeAnnee,
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
        pf.IncludeAnnee   = dto.IncludeAnnee;
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
            IncludeAnnee   = pf.IncludeAnnee,
        };
    }
}

public class EntrepriseSettingsDto
{
    public string RaisonSociale { get; set; } = "GestionCo. SARL";
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Ice { get; set; }
    public string? Rc { get; set; }
    public string? If { get; set; }
    public string? Patente { get; set; }
    public string? Cnss { get; set; }
    public string? Capital { get; set; }
    public string? Rib { get; set; }
    public string? Banque { get; set; }
    public string? Swift { get; set; }
    public string? Logo { get; set; }
}

public record GetEntrepriseSettingsQuery() : IRequest<EntrepriseSettingsDto>;
public record UpdateEntrepriseSettingsCommand(EntrepriseSettingsDto Dto) : IRequest<EntrepriseSettingsDto>;

public class GetEntrepriseSettingsHandler : IRequestHandler<GetEntrepriseSettingsQuery, EntrepriseSettingsDto>
{
    private readonly IAppDbContext _db;
    public GetEntrepriseSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<EntrepriseSettingsDto> Handle(GetEntrepriseSettingsQuery request, CancellationToken ct)
    {
        var pe = await _db.ParametresEntreprise.AsNoTracking().FirstOrDefaultAsync(ct);
        if (pe == null) return new EntrepriseSettingsDto();
        return new EntrepriseSettingsDto
        {
            RaisonSociale = pe.RaisonSociale,
            Adresse       = pe.Adresse,
            Telephone     = pe.Telephone,
            Email         = pe.Email,
            Ice           = pe.Ice,
            Rc            = pe.Rc,
            If            = pe.If,
            Patente       = pe.Patente,
            Cnss          = pe.Cnss,
            Capital       = pe.Capital,
            Rib           = pe.Rib,
            Banque        = pe.Banque,
            Swift         = pe.Swift,
            Logo          = pe.Logo,
        };
    }
}

public class UpdateEntrepriseSettingsHandler : IRequestHandler<UpdateEntrepriseSettingsCommand, EntrepriseSettingsDto>
{
    private readonly IAppDbContext _db;
    public UpdateEntrepriseSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<EntrepriseSettingsDto> Handle(UpdateEntrepriseSettingsCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var pe = await _db.ParametresEntreprise.FirstOrDefaultAsync(ct);
        if (pe == null)
        {
            pe = new ParametresEntrepriseEntity();
            _db.ParametresEntreprise.Add(pe);
        }

        pe.RaisonSociale = (dto.RaisonSociale ?? "GestionCo. SARL").Trim();
        pe.Adresse       = dto.Adresse?.Trim();
        pe.Telephone     = dto.Telephone?.Trim();
        pe.Email         = dto.Email?.Trim();
        pe.Ice           = dto.Ice?.Trim();
        pe.Rc            = dto.Rc?.Trim();
        pe.If            = dto.If?.Trim();
        pe.Patente       = dto.Patente?.Trim();
        pe.Cnss          = dto.Cnss?.Trim();
        pe.Capital       = dto.Capital?.Trim();
        pe.Rib           = dto.Rib?.Trim();
        pe.Banque        = dto.Banque?.Trim();
        pe.Swift         = dto.Swift?.Trim();
        pe.Logo          = dto.Logo?.Trim();
        pe.UpdatedAt     = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new EntrepriseSettingsDto
        {
            RaisonSociale = pe.RaisonSociale,
            Adresse       = pe.Adresse,
            Telephone     = pe.Telephone,
            Email         = pe.Email,
            Ice           = pe.Ice,
            Rc            = pe.Rc,
            If            = pe.If,
            Patente       = pe.Patente,
            Cnss          = pe.Cnss,
            Capital       = pe.Capital,
            Rib           = pe.Rib,
            Banque        = pe.Banque,
            Swift         = pe.Swift,
            Logo          = pe.Logo,
        };
    }
}

// ── SMTP SETTINGS ────────────────────────────────────────────────────────────

public class SmtpSettingsDto
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromName { get; set; } = "GestionCo. SARL";
    public string FromAddress { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
}

public record GetSmtpSettingsQuery() : IRequest<SmtpSettingsDto>;
public record UpdateSmtpSettingsCommand(SmtpSettingsDto Dto) : IRequest<SmtpSettingsDto>;
public record TestSmtpCommand(string ToEmail) : IRequest<string>;

public class GetSmtpSettingsHandler : IRequestHandler<GetSmtpSettingsQuery, SmtpSettingsDto>
{
    private readonly IAppDbContext _db;
    public GetSmtpSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<SmtpSettingsDto> Handle(GetSmtpSettingsQuery request, CancellationToken ct)
    {
        var ps = await _db.ParametresSmtp.AsNoTracking().FirstOrDefaultAsync(ct);
        if (ps == null) return new SmtpSettingsDto();
        return SmtpMapper.Map(ps);
    }
}

public class UpdateSmtpSettingsHandler : IRequestHandler<UpdateSmtpSettingsCommand, SmtpSettingsDto>
{
    private readonly IAppDbContext _db;
    public UpdateSmtpSettingsHandler(IAppDbContext db) => _db = db;

    public async Task<SmtpSettingsDto> Handle(UpdateSmtpSettingsCommand request, CancellationToken ct)
    {
        var dto = request.Dto;
        var ps = await _db.ParametresSmtp.FirstOrDefaultAsync(ct);
        if (ps == null) { ps = new ParametresSmtp(); _db.ParametresSmtp.Add(ps); }

        ps.Host        = (dto.Host ?? "").Trim();
        ps.Port        = dto.Port > 0 ? dto.Port : 587;
        ps.Username    = (dto.Username ?? "").Trim();
        ps.Password    = dto.Password ?? "";
        ps.FromName    = (dto.FromName ?? "GestionCo. SARL").Trim();
        ps.FromAddress = (dto.FromAddress ?? "").Trim();
        ps.EnableSsl   = dto.EnableSsl;
        ps.UpdatedAt   = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return SmtpMapper.Map(ps);
    }
}

public class TestSmtpHandler : IRequestHandler<TestSmtpCommand, string>
{
    private readonly IAppDbContext _db;
    public TestSmtpHandler(IAppDbContext db) => _db = db;

    public async Task<string> Handle(TestSmtpCommand request, CancellationToken ct)
    {
        var ps = await _db.ParametresSmtp.AsNoTracking().FirstOrDefaultAsync(ct);
        if (ps == null || string.IsNullOrWhiteSpace(ps.Host) || string.IsNullOrWhiteSpace(ps.Username))
            throw new BusinessException("SMTP non configuré. Renseignez d'abord les paramètres email.");

        var mime = new MimeKit.MimeMessage();
        mime.From.Add(new MimeKit.MailboxAddress(ps.FromName, ps.FromAddress));
        mime.To.Add(new MimeKit.MailboxAddress(request.ToEmail, request.ToEmail));
        mime.Subject = "Test SMTP — GestionCo.";
        mime.Body = new MimeKit.TextPart("plain") { Text = "Ce message confirme que votre configuration SMTP fonctionne correctement." };

        var socketOptions = ps.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            _ when ps.EnableSsl => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.None,
        };

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(ps.Host, ps.Port, socketOptions, ct);
        await smtpClient.AuthenticateAsync(ps.Username, ps.Password, ct);
        await smtpClient.SendAsync(mime, ct);
        await smtpClient.DisconnectAsync(true, ct);

        return $"Email de test envoyé à {request.ToEmail}";
    }
}

internal static class SmtpMapper
{
    internal static SmtpSettingsDto Map(ParametresSmtp ps) => new()
    {
        Host        = ps.Host,
        Port        = ps.Port,
        Username    = ps.Username,
        Password    = ps.Password,
        FromName    = ps.FromName,
        FromAddress = ps.FromAddress,
        EnableSsl   = ps.EnableSsl,
    };
}
