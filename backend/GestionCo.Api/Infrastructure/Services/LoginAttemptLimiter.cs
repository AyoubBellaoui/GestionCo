using GestionCo.Api.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GestionCo.Api.Infrastructure.Services;

// RG-S2 : limitation des mots de passe erronés sur une fenêtre glissante de 15 minutes.
//  - 5 échecs pour un même compte depuis une même adresse IP bloquent ce compte pour cette IP,
//    sans pénaliser les autres utilisateurs du même réseau (bureau derrière une seule IP publique) ;
//  - 20 échecs depuis une même IP, tous comptes confondus, bloquent l'IP (protection contre
//    le « password spraying » sur plusieurs comptes).
// Une connexion réussie remet à zéro le compteur du compte (pas celui de l'IP).
public class LoginAttemptLimiter : ILoginAttemptLimiter
{
    private const int MaxFailuresPerAccount = 5;
    private const int MaxFailuresPerIp = 20;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int Failures;
        public DateTimeOffset ResetAt;
    }

    private readonly IMemoryCache _cache;
    public LoginAttemptLimiter(IMemoryCache cache) => _cache = cache;

    private static string IpKey(string ip) => $"login-failures:ip:{ip}";
    private static string AccountKey(string ip, string email) =>
        $"login-failures:acct:{ip}:{email.Trim().ToLowerInvariant()}";

    public bool IsBlocked(string ip, string email, out TimeSpan retryAfter)
    {
        var acct = Remaining(AccountKey(ip, email), MaxFailuresPerAccount);
        var all  = Remaining(IpKey(ip), MaxFailuresPerIp);
        retryAfter = acct > all ? acct : all;
        return retryAfter > TimeSpan.Zero;
    }

    public void RecordFailure(string ip, string email)
    {
        Increment(AccountKey(ip, email));
        Increment(IpKey(ip));
    }

    public void Reset(string ip, string email) => _cache.Remove(AccountKey(ip, email));

    private TimeSpan Remaining(string key, int max)
    {
        if (!_cache.TryGetValue(key, out Entry? entry) || entry == null) return TimeSpan.Zero;
        lock (entry)
        {
            if (entry.Failures < max) return TimeSpan.Zero;
            var left = entry.ResetAt - DateTimeOffset.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }

    private void Increment(string key)
    {
        var entry = _cache.GetOrCreate(key, e =>
        {
            var resetAt = DateTimeOffset.UtcNow.Add(Window);
            e.AbsoluteExpiration = resetAt;
            return new Entry { ResetAt = resetAt };
        })!;
        lock (entry) entry.Failures++;
    }
}
