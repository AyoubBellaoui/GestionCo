namespace GestionCo.Api.Application.Produits.DTOs;

public class ProduitDto
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public string? CodeBarre { get; set; }
    public decimal PrixHT { get; set; }
    public decimal TVA { get; set; }
    public decimal PrixTTC { get; set; }
    public decimal PrixVenteHT { get; set; }
    public decimal TVAVente { get; set; }
    public decimal PrixVenteTTC { get; set; }
    public int QuantiteStock { get; set; }
    public int SeuilAlerte { get; set; }
    public int QuantiteReappro { get; set; }
    public bool IsStockFaible { get; set; }
    public bool IsRupture { get; set; }
    public int? CategorieId { get; set; }
    public string? CategorieNom { get; set; }
    public string? CategorieIcone { get; set; }
    public int? FournisseurId { get; set; }
    public string? FournisseurNom { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProduitDto
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Image { get; set; }
    public decimal PrixHT { get; set; }
    public decimal TVA { get; set; } = 20;
    public decimal PrixVenteHT { get; set; }
    public decimal TVAVente { get; set; } = 20;
    public int QuantiteStock { get; set; }
    public int SeuilAlerte { get; set; } = 5;
    public int QuantiteReappro { get; set; } = 0;
    public int? CategorieId { get; set; }
    public int? FournisseurId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProduitDto : CreateProduitDto
{
    public int Id { get; set; }
}
