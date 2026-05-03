using GestionCo.Api.Domain.Common;

namespace GestionCo.Api.Domain.Entities;

public class Categorie : AuditableEntity
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icone { get; set; } // emoji

    // Navigation
    public ICollection<Produit> Produits { get; set; } = new List<Produit>();
}
