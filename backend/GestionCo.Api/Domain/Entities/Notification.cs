using GestionCo.Api.Domain.Enums;

namespace GestionCo.Api.Domain.Entities;

public class Notification
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public TypeNotification Type { get; set; } = TypeNotification.Info;
    public CategorieNotification Categorie { get; set; } = CategorieNotification.Systeme;
    public bool IsRead { get; set; } = false;
    public int? EntiteId { get; set; }
    public string? EntiteReference { get; set; }
    public string? LienUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
