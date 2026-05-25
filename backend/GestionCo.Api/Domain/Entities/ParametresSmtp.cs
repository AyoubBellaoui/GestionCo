namespace GestionCo.Api.Domain.Entities;

public class ParametresSmtp
{
    public int Id { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromName { get; set; } = "GestionCo. SARL";
    public string FromAddress { get; set; } = "";
    public bool EnableSsl { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
