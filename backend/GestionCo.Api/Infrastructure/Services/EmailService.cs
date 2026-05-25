using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace GestionCo.Api.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IAppDbContext _db;
    private readonly IPdfService _pdf;

    public EmailService(IAppDbContext db, IPdfService pdf)
    {
        _db = db; _pdf = pdf;
    }

    private async Task<(string host, int port, bool enableSsl, string username, string password, string fromName, string fromAddr)>
        LoadSmtpConfig(CancellationToken ct)
    {
        var ps = await _db.ParametresSmtp.AsNoTracking().FirstOrDefaultAsync(ct);
        if (ps == null || string.IsNullOrWhiteSpace(ps.Host) || string.IsNullOrWhiteSpace(ps.Username))
            throw new BusinessException("Email non configuré. Allez dans Paramètres → Email et renseignez votre serveur SMTP.");

        return (ps.Host, ps.Port, ps.EnableSsl, ps.Username, ps.Password, ps.FromName, ps.FromAddress);
    }

    public async Task SendDevisAsync(int devisId, string toEmail, string? message = null, EntrepriseInfoDto? info = null, CancellationToken ct = default)
    {
        var devis = await _db.Devis
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == devisId, ct)
            ?? throw new NotFoundException("Devis", devisId);

        var (host, port, enableSsl, username, password, fromName, fromAddr) = await LoadSmtpConfig(ct);

        var pdfBytes = await _pdf.GenerateDevisPdfAsync(devisId, info, ct);

        var bodyText = $"""
            Bonjour {devis.Client.NomClient},

            Veuillez trouver ci-joint votre devis {devis.Reference}.

            {(string.IsNullOrWhiteSpace(message) ? "" : message + "\n\n")}Montant total TTC : {devis.MontantTotal:N2} MAD
            {(devis.DateValidite.HasValue ? $"Validité : {devis.DateValidite.Value:dd/MM/yyyy}" : "Validité illimitée")}

            Cordialement,
            {fromName}
            """;

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(fromName, fromAddr));
        mimeMessage.To.Add(new MailboxAddress(devis.Client.NomClient, toEmail));
        mimeMessage.Subject = $"Devis {devis.Reference} — {fromName}";

        var builder = new BodyBuilder { TextBody = bodyText };
        builder.Attachments.Add($"Devis-{devis.Reference}.pdf", pdfBytes, new ContentType("application", "pdf"));
        mimeMessage.Body = builder.ToMessageBody();

        await Send(host, port, enableSsl, username, password, mimeMessage, ct);
    }

    public async Task SendFactureAsync(int factureId, string toEmail, string? message = null, EntrepriseInfoDto? info = null, CancellationToken ct = default)
    {
        var facture = await _db.Factures
            .Include(f => f.Vente).ThenInclude(v => v.Client)
            .FirstOrDefaultAsync(f => f.Id == factureId, ct)
            ?? throw new NotFoundException("Facture", factureId);

        var (host, port, enableSsl, username, password, fromName, fromAddr) = await LoadSmtpConfig(ct);

        var pdfBytes = await _pdf.GenerateInvoicePdfAsync(factureId, info, ct);
        var client   = facture.Vente.Client;
        var reste    = facture.Vente.MontantTotal - facture.Vente.MontantPaye;

        var bodyText = $"""
            Bonjour {client.NomClient},

            Veuillez trouver ci-joint votre facture {facture.NumeroFacture}.

            {(string.IsNullOrWhiteSpace(message) ? "" : message + "\n\n")}Montant total TTC : {facture.Vente.MontantTotal:N2} MAD
            Montant réglé     : {facture.Vente.MontantPaye:N2} MAD
            Reste à payer     : {reste:N2} MAD
            Date d'échéance   : {facture.DateEcheance:dd/MM/yyyy}

            Cordialement,
            {fromName}
            """;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(fromName, fromAddr));
        mime.To.Add(new MailboxAddress(client.NomClient, toEmail));
        mime.Subject = $"Facture {facture.NumeroFacture} — {fromName}";

        var builder = new BodyBuilder { TextBody = bodyText };
        builder.Attachments.Add($"Facture-{facture.NumeroFacture}.pdf", pdfBytes, new ContentType("application", "pdf"));
        mime.Body = builder.ToMessageBody();

        await Send(host, port, enableSsl, username, password, mime, ct);

        facture.EstEnvoyeeEmail = true;
        facture.DateEnvoiEmail  = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static SecureSocketOptions GetSocketOptions(bool enableSsl, int port) => port switch
    {
        465 => SecureSocketOptions.SslOnConnect,
        _ when enableSsl => SecureSocketOptions.StartTls,
        _ => SecureSocketOptions.None,
    };

    private static async Task Send(string host, int port, bool enableSsl, string username, string password, MimeMessage mime, CancellationToken ct)
    {
        try
        {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(host, port, GetSocketOptions(enableSsl, port), ct);
            await smtpClient.AuthenticateAsync(username, password, ct);
            await smtpClient.SendAsync(mime, ct);
            await smtpClient.DisconnectAsync(true, ct);
        }
        catch (AuthenticationException ex)
        {
            throw new BusinessException($"Authentification SMTP échouée : {ex.Message}");
        }
        catch (SmtpCommandException ex)
        {
            throw new BusinessException($"Erreur SMTP ({ex.StatusCode}) : {ex.Message}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new BusinessException($"Connexion SMTP impossible ({host}:{port}) : {ex.Message}");
        }
    }
}
