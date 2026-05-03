using FluentValidation;
using GestionCo.Api.Application.Common.Exceptions;
using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Entities;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Utilisateurs;

// ============ DTOs ============
public class UtilisateurDto
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public RoleUtilisateur Role { get; set; }
    public string RoleLibelle => Role.ToString();
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string NomComplet => $"{Prenom} {Nom}".Trim();
    public string Initiales => $"{(Prenom.Length > 0 ? Prenom[0] : ' ')}{(Nom.Length > 0 ? Nom[0] : ' ')}".Trim().ToUpper();
}

public class CreateUtilisateurDto
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Gestionnaire;
    public string? Telephone { get; set; }
}

public class UpdateUtilisateurDto
{
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string? Telephone { get; set; }
    public RoleUtilisateur Role { get; set; }
    public bool IsActive { get; set; } = true;
}

// ============ GET ALL ============
public record GetUtilisateursQuery : IRequest<List<UtilisateurDto>>;

public class GetUtilisateursHandler : IRequestHandler<GetUtilisateursQuery, List<UtilisateurDto>>
{
    private readonly IAppDbContext _db;
    public GetUtilisateursHandler(IAppDbContext db) => _db = db;

    public async Task<List<UtilisateurDto>> Handle(GetUtilisateursQuery _, CancellationToken ct)
    {
        var users = await _db.Utilisateurs.OrderBy(u => u.Nom).ThenBy(u => u.Prenom).ToListAsync(ct);
        return users.Select(ToDto).ToList();
    }

    internal static UtilisateurDto ToDto(Utilisateur u) => new()
    {
        Id = u.Id, Nom = u.Nom, Prenom = u.Prenom,
        Email = u.Email, Telephone = u.Telephone,
        Role = u.Role, IsActive = u.IsActive, LastLoginAt = u.LastLoginAt,
    };
}

// ============ CREATE ============
public record CreateUtilisateurCommand(CreateUtilisateurDto Dto) : IRequest<UtilisateurDto>;

public class CreateUtilisateurValidator : AbstractValidator<CreateUtilisateurCommand>
{
    public CreateUtilisateurValidator()
    {
        RuleFor(x => x.Dto.Nom).NotEmpty().WithMessage("Nom requis");
        RuleFor(x => x.Dto.Prenom).NotEmpty().WithMessage("Prénom requis");
        RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().WithMessage("Email valide requis");
        RuleFor(x => x.Dto.Password).MinimumLength(6).WithMessage("Mot de passe min. 6 caractères");
    }
}

public class CreateUtilisateurHandler : IRequestHandler<CreateUtilisateurCommand, UtilisateurDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;

    public CreateUtilisateurHandler(IAppDbContext db, IPasswordHasher hasher, IAuditLogger audit)
        => (_db, _hasher, _audit) = (db, hasher, audit);

    public async Task<UtilisateurDto> Handle(CreateUtilisateurCommand req, CancellationToken ct)
    {
        var dto = req.Dto;

        if (await _db.Utilisateurs.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower(), ct))
            throw new BusinessException($"Un compte avec l'email '{dto.Email}' existe déjà");

        var user = new Utilisateur
        {
            Nom = dto.Nom.Trim(),
            Prenom = dto.Prenom.Trim(),
            Email = dto.Email.Trim().ToLower(),
            PasswordHash = _hasher.Hash(dto.Password),
            Role = dto.Role,
            Telephone = dto.Telephone?.Trim(),
            IsActive = true,
        };

        _db.Utilisateurs.Add(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Create, "utilisateurs",
            $"Compte créé : {user.Prenom} {user.Nom} ({user.Email}) — Rôle : {user.Role}",
            user.Id, user.Email, estSensible: true, ct: ct);

        return GetUtilisateursHandler.ToDto(user);
    }
}

// ============ UPDATE ============
public record UpdateUtilisateurCommand(int Id, UpdateUtilisateurDto Dto) : IRequest<UtilisateurDto>;

public class UpdateUtilisateurHandler : IRequestHandler<UpdateUtilisateurCommand, UtilisateurDto>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUserService _current;

    public UpdateUtilisateurHandler(IAppDbContext db, IAuditLogger audit, ICurrentUserService current)
        => (_db, _audit, _current) = (db, audit, current);

    public async Task<UtilisateurDto> Handle(UpdateUtilisateurCommand req, CancellationToken ct)
    {
        var user = await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == req.Id, ct)
            ?? throw new NotFoundException("Utilisateur", req.Id);

        if (!req.Dto.IsActive && user.Id == _current.UserId)
            throw new BusinessException("Vous ne pouvez pas désactiver votre propre compte");

        user.Nom = req.Dto.Nom.Trim();
        user.Prenom = req.Dto.Prenom.Trim();
        user.Telephone = req.Dto.Telephone?.Trim();
        user.Role = req.Dto.Role;
        user.IsActive = req.Dto.IsActive;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Update, "utilisateurs",
            $"Compte modifié : {user.Prenom} {user.Nom} ({user.Email})",
            user.Id, user.Email, ct: ct);

        return GetUtilisateursHandler.ToDto(user);
    }
}

// ============ DELETE ============
public record DeleteUtilisateurCommand(int Id) : IRequest<Unit>;

public class DeleteUtilisateurHandler : IRequestHandler<DeleteUtilisateurCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUserService _current;

    public DeleteUtilisateurHandler(IAppDbContext db, IAuditLogger audit, ICurrentUserService current)
        => (_db, _audit, _current) = (db, audit, current);

    public async Task<Unit> Handle(DeleteUtilisateurCommand req, CancellationToken ct)
    {
        var user = await _db.Utilisateurs.FirstOrDefaultAsync(u => u.Id == req.Id, ct)
            ?? throw new NotFoundException("Utilisateur", req.Id);

        if (user.Id == _current.UserId)
            throw new BusinessException("Vous ne pouvez pas supprimer votre propre compte");

        _db.Utilisateurs.Remove(user);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(ActionLog.Delete, "utilisateurs",
            $"Compte supprimé : {user.Prenom} {user.Nom} ({user.Email})",
            user.Id, user.Email, estSensible: true, ct: ct);

        return Unit.Value;
    }
}
