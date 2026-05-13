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
    private readonly IConfiguration _config;

    public EmailService(IAppDbContext db, IPdfService pdf, IConfiguration config)
    {
        _db = db; _pdf = pdf; _config = config;
    }

    public async Task SendDevisAsync(int devisId, string toEmail, string? message = null, EntrepriseInfoDto? info = null, SmtpConfigDto? smtp = null, CancellationToken ct = default)
    {
        var devis = await _db.Devis
            .Include(d => d.Client)
            .FirstOrDefaultAsync(d => d.Id == devisId, ct)
            ?? throw new NotFoundException("Devis", devisId);

        // Use SMTP from request (settings UI), fallback to appsettings.json
        var host     = smtp?.Host        ?? _config["Smtp:Host"]        ?? "smtp.gmail.com";
        var port     = smtp?.Port > 0    ? smtp.Port : int.Parse(_config["Smtp:Port"] ?? "587");
        var username = smtp?.Username    ?? _config["Smtp:Username"]    ?? "";
        var password = smtp?.Password    ?? _config["Smtp:Password"]    ?? "";
        var fromName = smtp?.FromName    ?? _config["Smtp:FromName"]    ?? "GestionCo. SARL";
        var fromAddr = smtp?.FromAddress ?? _config["Smtp:FromAddress"] ?? username;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new BusinessException("Email non configuré. Allez dans Paramètres → Email et renseignez votre adresse et mot de passe.");

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

        try
        {
            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await smtpClient.AuthenticateAsync(username, password, ct);
            await smtpClient.SendAsync(mimeMessage, ct);
            await smtpClient.DisconnectAsync(true, ct);
        }
        catch (AuthenticationException ex)
        {
            throw new BusinessException($"Mot de passe incorrect ou App Password requis. ({ex.Message})");
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
