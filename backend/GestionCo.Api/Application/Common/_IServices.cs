using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    RoleUtilisateur? Role { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(RoleUtilisateur role);
}

public interface IJwtService
{
    string GenerateAccessToken(Utilisateur user);
    string GenerateRefreshToken();
    int? ValidateAccessToken(string token);
    int? ExtractUserIdIgnoreExpiry(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IReferenceGenerator
{
    Task<string> GenerateProductReferenceAsync(CancellationToken ct = default);
    Task<string> GenerateSaleReferenceAsync(CancellationToken ct = default);
    Task<string> GeneratePurchaseReferenceAsync(CancellationToken ct = default);
    Task<string> GenerateInvoiceReferenceAsync(CancellationToken ct = default);
    Task<string> GenerateChargeReferenceAsync(CancellationToken ct = default);
    Task<string> GenerateDevisReferenceAsync(CancellationToken ct = default);
}

public interface INotificationService
{
    Task CreateAsync(
        string titre,
        string message,
        TypeNotification type = TypeNotification.Info,
        CategorieNotification categorie = CategorieNotification.Systeme,
        int? entiteId = null,
        string? entiteReference = null,
        string? lienUrl = null,
        CancellationToken ct = default);

    Task CreateStockAlertAsync(int produitId, string nomProduit, int quantiteStock, int seuilAlerte, CancellationToken ct = default);
}

public interface IAuditLogger
{
    Task LogAsync(
        ActionLog action,
        string entite,
        string description,
        int? entiteId = null,
        string? entiteReference = null,
        object? anciennesValeurs = null,
        object? nouvellesValeurs = null,
        bool estSensible = false,
        CancellationToken ct = default,
        int? utilisateurId = null);
}

public class EntrepriseInfoDto
{
    public string? RaisonSociale { get; set; }
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

public interface IPdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(int factureId, EntrepriseInfoDto? info = null, CancellationToken ct = default);
    Task<byte[]> GenerateDevisPdfAsync(int devisId, EntrepriseInfoDto? info = null, CancellationToken ct = default);
    byte[] GeneratePLReportPdf(GestionCo.Api.Application.Reports.PLReportDto report, string entreprise);
    byte[] GenerateTVAReportPdf(GestionCo.Api.Application.Reports.TVAReportDto report, string entreprise);
}

public interface IEmailService
{
    Task SendDevisAsync(int devisId, string toEmail, string? message = null, EntrepriseInfoDto? info = null, CancellationToken ct = default);
    Task SendFactureAsync(int factureId, string toEmail, string? message = null, EntrepriseInfoDto? info = null, CancellationToken ct = default);
}

public class ReapproResultDto
{
    public bool Created { get; set; }
    public int AchatId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public interface IReapproService
{
    Task<ReapproResultDto> TryGenererReapproAsync(int produitId, int utilisateurId, CancellationToken ct = default);
}
