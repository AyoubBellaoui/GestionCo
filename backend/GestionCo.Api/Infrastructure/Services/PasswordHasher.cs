using GestionCo.Api.Application.Common.Interfaces;

namespace GestionCo.Api.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
