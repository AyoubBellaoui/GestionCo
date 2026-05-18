namespace GestionCo.Api.Domain.Entities;

public class ParametresFacturation
{
    public int Id { get; set; }
    public string PrefixeFacture { get; set; } = "FAC";
    public string PrefixeVente { get; set; } = "VNT";
    public string PrefixeAchat { get; set; } = "ACH";
    public string PrefixeProduit { get; set; } = "PRD";
    public int TvaParDefaut { get; set; } = 20;
    public int DelaiPaiement { get; set; } = 30;
    public bool IncludeAnnee { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
