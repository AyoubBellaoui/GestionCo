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
        CancellationToken ct = default);
}

public interface IPdfService
{
    Task<byte[]> GenerateInvoicePdfAsync(int factureId, CancellationToken ct = default);
    byte[] GeneratePLReportPdf(GestionCo.Api.Application.Reports.PLReportDto report, string entreprise);
    byte[] GenerateTVAReportPdf(GestionCo.Api.Application.Reports.TVAReportDto report, string entreprise);
}
