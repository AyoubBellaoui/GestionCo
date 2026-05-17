using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Application.Common.Models;
using GestionCo.Api.Application.Produits.DTOs;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Produits.Commands;

// ============ CREATE ============
public record CreateProduitCommand(CreateProduitDto Dto) : IRequest<ProduitDto>;

public class CreateProduitValidator : AbstractValidator<CreateProduitCommand>
{
    public CreateProduitValidator()
    {
        RuleFor(x => x.Dto.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.PrixHT).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.TVA).InclusiveBetween(0, 30);
        RuleFor(x => x.Dto.QuantiteStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Dto.SeuilAlerte).GreaterThanOrEqualTo(0);
    }
}

public class CreateProduitHandler : IRequestHandler<CreateProduitCommand, ProduitDto>
{
    private readonly IAppDbContext _db;
    private readonly IReferenceGenerator _refGen;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUserService _current;

    public CreateProduitHandler(IAppDbContext db, IReferenceGenerator refGen, IAuditLogger audit, ICurrentUserService current)
    {
        _db = db; _refGen = refGen; _audit = audit; _current = current;
    }

    public async Task<ProduitDto> Handle(CreateProduitCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var reference = await _refGen.GenerateProductReferenceAsync(ct);
        var codeBarre = GenerateBarcode();

        var produit = new Produit
        {
            Reference = reference,
            Nom = dto.Nom.Trim(),
            Description = dto.Description?.Trim(),
            Image = dto.Image?.Trim(),
            CodeBarre = codeBarre,
            PrixHT = dto.PrixHT,
            TVA = dto.TVA,
            PrixTTC = Math.Round(dto.PrixHT * (1 + dto.TVA / 100), 2),
            PrixVenteHT = dto.PrixVenteHT,
            TVAVente = dto.TVAVente,
            PrixVenteTTC = Math.Round(dto.PrixVenteHT * (1 + dto.TVAVente / 100), 2),
            QuantiteStock = dto.QuantiteStock,
            SeuilAlerte = dto.SeuilAlerte,
            QuantiteReappro = dto.QuantiteReappro,
            CategorieId = dto.CategorieId,
            FournisseurId = dto.FournisseurId,
            IsActive = dto.IsActive
        };

        _db.Produits.Add(produit);
        await _db.SaveChangesAsync(ct);

        // Mouvement stock initial si stock > 0
        if (produit.QuantiteStock > 0 && _current.UserId.HasValue)
        {
            _db.MouvementsStock.Add(new MouvementStock
            {
                ProduitId = produit.Id,
                Type = TypeMouvementStock.Entree,
                Quantite = produit.QuantiteStock,
                StockAvant = 0,
                StockApres = produit.QuantiteStock,
                Source = SourceMouvementStock.Manuel,
                UtilisateurId = _current.UserId.Value,
                Raison = "Stock initial à la création",
                DateMouvement = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync(
            ActionLog.Create, "produits",
            $"Produit créé : {produit.Nom} ({produit.Reference})",
            produit.Id, produit.Reference,
            nouvellesValeurs: new { produit.Nom, produit.PrixHT, produit.QuantiteStock },
            ct: ct);

        return await MapToDto(produit.Id, ct);
    }

    private async Task<ProduitDto> MapToDto(int id, CancellationToken ct)
    {
        var p = await _db.Produits
            .Include(p => p.Categorie).Include(p => p.Fournisseur)
            .FirstAsync(p => p.Id == id, ct);
        return ProduitMapper.ToDto(p);
    }

    private static string GenerateBarcode()
    {
        var rnd = new Random();
        return "611" + DateTime.UtcNow.Year.ToString().Substring(2) +
               rnd.Next(1000000, 9999999).ToString();
    }
}

// ============ BULK IMPORT ============
public record BulkImportProduitsCommand(List<CreateProduitDto> Items) : IRequest<BulkImportResultDto>;

public class BulkImportProduitsHandler : IRequestHandler<BulkImportProduitsCommand, BulkImportResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUserService _current;

    public BulkImportProduitsHandler(IAppDbContext db, IAuditLogger audit, ICurrentUserService current)
    {
        _db = db; _audit = audit; _current = current;
    }

    public async Task<BulkImportResultDto> Handle(BulkImportProduitsCommand req, CancellationToken ct)
    {
        var result = new BulkImportResultDto();
        if (req.Items.Count == 0) return result;
        if (req.Items.Count > 500) throw new BusinessException("Maximum 500 lignes par import");

        var pf = await _db.ParametresFacturation.AsNoTracking().FirstOrDefaultAsync(ct)
                 ?? new ParametresFacturation();
        var year = DateTime.UtcNow.Year;
        var prefix = $"{pf.PrefixeProduit}-{year}-";

        var lastRef = await _db.Produits
            .Where(p => p.Reference.StartsWith(prefix))
            .OrderByDescending(p => p.Reference)
            .Select(p => p.Reference)
            .FirstOrDefaultAsync(ct);

        var nextNum = string.IsNullOrEmpty(lastRef) ? 1
            : int.TryParse(lastRef[prefix.Length..], out var n) ? n + 1 : 1;

        var toAdd = new List<Produit>();

        for (int i = 0; i < req.Items.Count; i++)
        {
            var dto = req.Items[i];
            var row = i + 2;

            if (string.IsNullOrWhiteSpace(dto.Nom))
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom requis" }); continue; }
            if (dto.Nom.Length > 200)
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Nom trop long (max 200 caractères)" }); continue; }
            if (dto.PrixHT < 0)
            { result.Errors.Add(new BulkImportRowError { Row = row, Message = "Prix HT invalide" }); continue; }

            var tva = dto.TVA == 0 ? 20m : dto.TVA;
            var tvaVente = dto.TVAVente == 0 ? 20m : dto.TVAVente;

            toAdd.Add(new Produit
            {
                Reference = $"{prefix}{nextNum++:D4}",
                CodeBarre = GenerateBarcode(),
                Nom = dto.Nom.Trim(),
                Description = dto.Description?.Trim(),
                PrixHT = dto.PrixHT,
                TVA = tva,
                PrixTTC = Math.Round(dto.PrixHT * (1 + tva / 100), 2),
                PrixVenteHT = dto.PrixVenteHT,
                TVAVente = tvaVente,
                PrixVenteTTC = Math.Round(dto.PrixVenteHT * (1 + tvaVente / 100), 2),
                QuantiteStock = dto.QuantiteStock,
                SeuilAlerte = dto.SeuilAlerte > 0 ? dto.SeuilAlerte : 5,
                QuantiteReappro = dto.QuantiteReappro,
                IsActive = dto.IsActive,
            });
        }

        if (toAdd.Count > 0)
        {
            _db.Produits.AddRange(toAdd);
            await _db.SaveChangesAsync(ct);

            if (_current.UserId.HasValue)
            {
                foreach (var p in toAdd.Where(p => p.QuantiteStock > 0))
                {
                    _db.MouvementsStock.Add(new MouvementStock
                    {
                        ProduitId = p.Id,
                        Type = TypeMouvementStock.Entree,
                        Quantite = p.QuantiteStock,
                        StockAvant = 0,
                        StockApres = p.QuantiteStock,
                        Source = SourceMouvementStock.Manuel,
                        UtilisateurId = _current.UserId.Value,
                        Raison = "Stock initial — import",
                        DateMouvement = DateTime.UtcNow
                    });
                }
                await _db.SaveChangesAsync(ct);
            }

            await _audit.LogAsync(ActionLog.Create, "produits",
                $"Import massif : {toAdd.Count} produit(s) créé(s)", ct: ct);
        }

        result.Imported = toAdd.Count;
        result.Failed = result.Errors.Count;
        return result;
    }

    private static string GenerateBarcode()
        => "611" + DateTime.UtcNow.Year.ToString()[2..] + Random.Shared.Next(1000000, 9999999);
}

// ============ UPDATE ============
public record UpdateProduitCommand(UpdateProduitDto Dto) : IRequest<ProduitDto>;

public class UpdateProduitValidator : AbstractValidator<UpdateProduitCommand>
{
    public UpdateProduitValidator()
    {
        RuleFor(x => x.Dto.Id).GreaterThan(0);
        RuleFor(x => x.Dto.Nom).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dto.PrixHT).GreaterThanOrEqualTo(0);
    }
}

public class UpdateProduitHandler : IRequestHandler<UpdateProduitCommand, ProduitDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public UpdateProduitHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db; _audit = audit;
    }

    public async Task<ProduitDto> Handle(UpdateProduitCommand req, CancellationToken ct)
    {
        var dto = req.Dto;
        var produit = await _db.Produits
            .Include(p => p.Categorie).Include(p => p.Fournisseur)
            .FirstOrDefaultAsync(p => p.Id == dto.Id, ct)
            ?? throw new NotFoundException("Produit", dto.Id);

        var ancien = new { produit.Nom, produit.PrixHT, produit.QuantiteStock, produit.SeuilAlerte };

        produit.Nom = dto.Nom.Trim();
        produit.Description = dto.Description?.Trim();
        produit.Image = dto.Image?.Trim();
        produit.PrixHT = dto.PrixHT;
        produit.TVA = dto.TVA;
        produit.PrixTTC = Math.Round(dto.PrixHT * (1 + dto.TVA / 100), 2);
        produit.PrixVenteHT = dto.PrixVenteHT;
        produit.TVAVente = dto.TVAVente;
        produit.PrixVenteTTC = Math.Round(dto.PrixVenteHT * (1 + dto.TVAVente / 100), 2);
        produit.SeuilAlerte = dto.SeuilAlerte;
        produit.QuantiteReappro = dto.QuantiteReappro;
        produit.CategorieId = dto.CategorieId;
        produit.FournisseurId = dto.FournisseurId;
        produit.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            ActionLog.Update, "produits",
            $"Produit modifié : {produit.Nom} ({produit.Reference})",
            produit.Id, produit.Reference,
            anciennesValeurs: ancien,
            nouvellesValeurs: new { produit.Nom, produit.PrixHT, produit.QuantiteStock, produit.SeuilAlerte },
            ct: ct);

        return ProduitMapper.ToDto(produit);
    }
}

// ============ DELETE ============
public record DeleteProduitCommand(int Id) : IRequest<Unit>;

public class DeleteProduitHandler : IRequestHandler<DeleteProduitCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;

    public DeleteProduitHandler(IAppDbContext db, IAuditLogger audit)
    {
        _db = db; _audit = audit;
    }

    public async Task<Unit> Handle(DeleteProduitCommand req, CancellationToken ct)
    {
        var produit = await _db.Produits.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException("Produit", req.Id);

        // Soft delete : désactiver au lieu de supprimer (pour préserver intégrité références)
        var hasUsage = await _db.LignesVente.AnyAsync(l => l.ProduitId == req.Id, ct)
                       || await _db.LignesAchat.AnyAsync(l => l.ProduitId == req.Id, ct);

        if (hasUsage)
        {
            produit.IsActive = false;
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            _db.Produits.Remove(produit);
            await _db.SaveChangesAsync(ct);
        }

        await _audit.LogAsync(
            ActionLog.Delete, "produits",
            $"Produit {(hasUsage ? "désactivé" : "supprimé")} : {produit.Nom} ({produit.Reference})",
            produit.Id, produit.Reference,
            estSensible: true, ct: ct);

        return Unit.Value;
    }
}

// ============ RÉAPPRO ============
public record GenererReapproCommand(int ProduitId) : IRequest<ReapproResultDto>;

public class GenererReapproHandler : IRequestHandler<GenererReapproCommand, ReapproResultDto>
{
    private readonly IReapproService _reappro;
    private readonly ICurrentUserService _current;

    public GenererReapproHandler(IReapproService reappro, ICurrentUserService current)
    {
        _reappro = reappro; _current = current;
    }

    public async Task<ReapproResultDto> Handle(GenererReapproCommand req, CancellationToken ct)
    {
        var userId = _current.UserId ?? throw new UnauthorizedException();
        var result = await _reappro.TryGenererReapproAsync(req.ProduitId, userId, ct);
        if (!result.Created && result.AchatId == 0)
            throw new BusinessException(result.Message ?? "Réapprovisionnement impossible");
        return result;
    }
}

// ============ MAPPER ============
public static class ProduitMapper
{
    public static ProduitDto ToDto(Produit p) => new()
    {
        Id = p.Id,
        Reference = p.Reference,
        Nom = p.Nom,
        Description = p.Description,
        Image = p.Image,
        CodeBarre = p.CodeBarre,
        PrixHT = p.PrixHT,
        TVA = p.TVA,
        PrixTTC = p.PrixTTC,
        PrixVenteHT = p.PrixVenteHT,
        TVAVente = p.TVAVente,
        PrixVenteTTC = p.PrixVenteTTC,
        QuantiteStock = p.QuantiteStock,
        SeuilAlerte = p.SeuilAlerte,
        QuantiteReappro = p.QuantiteReappro,
        IsStockFaible = p.IsStockFaible,
        IsRupture = p.IsRupture,
        CategorieId = p.CategorieId,
        CategorieNom = p.Categorie?.Nom,
        CategorieIcone = p.Categorie?.Icone,
        FournisseurId = p.FournisseurId,
        FournisseurNom = p.Fournisseur?.Nom,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };
}
