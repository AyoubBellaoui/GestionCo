using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GestionCo.Api.Application.Notifications;

// ============ DTOs ============
public class NotificationDto
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TypeNotification Type { get; set; }
    public string TypeLibelle => Type.ToString();
    public CategorieNotification Categorie { get; set; }
    public string CategorieLibelle => Categorie.ToString();
    public bool IsRead { get; set; }
    public int? EntiteId { get; set; }
    public string? EntiteReference { get; set; }
    public string? LienUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TempsEcoule { get; set; } = string.Empty;
}

public class NotificationSummaryDto
{
    public int UnreadCount { get; set; }
    public List<NotificationDto> Recent { get; set; } = new();
}

// ============ COMMANDS ============
public record MarkNotificationReadCommand(int Id) : IRequest;
public record MarkAllNotificationsReadCommand : IRequest;
public record DeleteNotificationCommand(int Id) : IRequest;
public record DeleteAllReadNotificationsCommand : IRequest;

// ============ HANDLERS ============
public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IAppDbContext _db;
    public MarkNotificationReadHandler(IAppDbContext db) => _db = db;

    public async Task Handle(MarkNotificationReadCommand req, CancellationToken ct)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == req.Id, ct);
        if (notif == null) return;
        notif.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IAppDbContext _db;
    public MarkAllNotificationsReadHandler(IAppDbContext db) => _db = db;

    public async Task Handle(MarkAllNotificationsReadCommand req, CancellationToken ct)
    {
        var unread = await _db.Notifications.Where(n => !n.IsRead).ToListAsync(ct);
        foreach (var n in unread) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}

public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand>
{
    private readonly IAppDbContext _db;
    public DeleteNotificationHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteNotificationCommand req, CancellationToken ct)
    {
        var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == req.Id, ct);
        if (notif == null) return;
        _db.Notifications.Remove(notif);
        await _db.SaveChangesAsync(ct);
    }
}

public class DeleteAllReadNotificationsHandler : IRequestHandler<DeleteAllReadNotificationsCommand>
{
    private readonly IAppDbContext _db;
    public DeleteAllReadNotificationsHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteAllReadNotificationsCommand req, CancellationToken ct)
    {
        var read = await _db.Notifications.Where(n => n.IsRead).ToListAsync(ct);
        _db.Notifications.RemoveRange(read);
        await _db.SaveChangesAsync(ct);
    }
}

// ============ QUERIES ============
public record GetNotificationsQuery(int Limit = 30) : IRequest<NotificationSummaryDto>;

public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, NotificationSummaryDto>
{
    private readonly IAppDbContext _db;
    public GetNotificationsHandler(IAppDbContext db) => _db = db;

    public async Task<NotificationSummaryDto> Handle(GetNotificationsQuery q, CancellationToken ct)
    {
        var unreadCount = await _db.Notifications.CountAsync(n => !n.IsRead, ct);

        var recent = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(q.Limit)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;

        return new NotificationSummaryDto
        {
            UnreadCount = unreadCount,
            Recent = recent.Select(n => new NotificationDto
            {
                Id = n.Id,
                Titre = n.Titre,
                Message = n.Message,
                Type = n.Type,
                Categorie = n.Categorie,
                IsRead = n.IsRead,
                EntiteId = n.EntiteId,
                EntiteReference = n.EntiteReference,
                LienUrl = n.LienUrl,
                CreatedAt = n.CreatedAt,
                TempsEcoule = FormatTimeAgo(now - n.CreatedAt)
            }).ToList()
        };
    }

    private static string FormatTimeAgo(TimeSpan diff)
    {
        if (diff.TotalSeconds < 60) return "À l'instant";
        if (diff.TotalMinutes < 60) return $"Il y a {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"Il y a {(int)diff.TotalHours}h";
        if (diff.TotalDays < 7) return $"Il y a {(int)diff.TotalDays} jour{((int)diff.TotalDays > 1 ? "s" : "")}";
        return (DateTime.UtcNow - diff).ToString("dd/MM/yyyy");
    }
}
