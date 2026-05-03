using GestionCo.Api.Domain.Common;

namespace GestionCo.Api.Domain.Entities;

public class Fournisseur : AuditableEntity
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Icone { get; set; } // emoji 💻 🪑 📦...
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? SiteWeb { get; set; }
    public string? PersonneContact { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Produit> Produits { get; set; } = new List<Produit>();
    public ICollection<Achat> Achats { get; set; } = new List<Achat>();
}
