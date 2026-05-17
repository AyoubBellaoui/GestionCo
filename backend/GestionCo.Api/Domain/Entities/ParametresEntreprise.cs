namespace GestionCo.Api.Domain.Entities;

public class ParametresEntreprise
{
    public int Id { get; set; }
    public string RaisonSociale { get; set; } = "GestionCo. SARL";
    public string? Adresse { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Ice { get; set; }
    public string? Rc { get; set; }
    public string? If { get; set; }
    public string? Patente { get; set; }
    public string? Cnss { get; set; }
    public string? Capital { get; set; }
    public string? Rib { get; set; }
    public string? Banque { get; set; }
    public string? Swift { get; set; }
    public string? Logo { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
