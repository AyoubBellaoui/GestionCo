using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Categories;

public class CategorieDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icone { get; set; }
    public int NombreProduits { get; set; }
}

public class CreateCategorieDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icone { get; set; } = "📦";
}

public record GetCategoriesQuery : IRequest<List<CategorieDto>>;
public record CreateCategorieCommand(CreateCategorieDto Dto) : IRequest<CategorieDto>;
public record UpdateCategorieCommand(int Id, CreateCategorieDto Dto) : IRequest<CategorieDto>;
public record DeleteCategorieCommand(int Id) : IRequest<Unit>;

public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, List<CategorieDto>>
{
    private readonly IAppDbContext _db;
    public GetCategoriesHandler(IAppDbContext db) => _db = db;

    public async Task<List<CategorieDto>> Handle(GetCategoriesQuery q, CancellationToken ct)
    {
        return await _db.Categories
            .OrderBy(c => c.Nom)
            .Select(c => new CategorieDto
            {
                Id = c.Id, Nom = c.Nom,
                Description = c.Description, Icone = c.Icone,
                NombreProduits = c.Produits.Count
            }).ToListAsync(ct);
    }
}

public class CreateCategorieHandler : IRequestHandler<CreateCategorieCommand, CategorieDto>
{
    private readonly IAppDbContext _db;
    public CreateCategorieHandler(IAppDbContext db) => _db = db;

    public async Task<CategorieDto> Handle(CreateCategorieCommand req, CancellationToken ct)
    {
        var c = new Categorie
        {
            Nom = req.Dto.Nom.Trim(),
            Description = req.Dto.Description?.Trim(),
            Icone = req.Dto.Icone
        };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync(ct);
        return new CategorieDto { Id = c.Id, Nom = c.Nom, Description = c.Description, Icone = c.Icone };
    }
}

public class UpdateCategorieHandler : IRequestHandler<UpdateCategorieCommand, CategorieDto>
{
    private readonly IAppDbContext _db;
    public UpdateCategorieHandler(IAppDbContext db) => _db = db;

    public async Task<CategorieDto> Handle(UpdateCategorieCommand req, CancellationToken ct)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Catégorie", req.Id);

        c.Nom = req.Dto.Nom.Trim();
        c.Description = req.Dto.Description?.Trim();
        c.Icone = req.Dto.Icone;
        await _db.SaveChangesAsync(ct);

        return new CategorieDto { Id = c.Id, Nom = c.Nom, Description = c.Description, Icone = c.Icone };
    }
}

public class DeleteCategorieHandler : IRequestHandler<DeleteCategorieCommand, Unit>
{
    private readonly IAppDbContext _db;
    public DeleteCategorieHandler(IAppDbContext db) => _db = db;

    public async Task<Unit> Handle(DeleteCategorieCommand req, CancellationToken ct)
    {
        var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == req.Id, ct)
            ?? throw new NotFoundException("Catégorie", req.Id);
        _db.Categories.Remove(c);
        await _db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
