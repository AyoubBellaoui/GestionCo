using GestionCo.Api.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace GestionCo.Api.Infrastructure.Services;

// RG-S2 : au plus 5 mots de passe erronés par adresse IP sur une fenêtre de 15 minutes.
// Les connexions réussies ne sont jamais bloquées et remettent le compteur à zéro.
public class LoginAttemptLimiter : ILoginAttemptLimiter
{
    private const int MaxFailures = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public int Failures;
        public DateTimeOffset ResetAt;
    }

    private readonly IMemoryCache _cache;
    public LoginAttemptLimiter(IMemoryCache cache) => _cache = cache;

    private static string Key(string ip) => $"login-failures:{ip}";

    public bool IsBlocked(string ip, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        if (!_cache.TryGetValue(Key(ip), out Entry? entry) || entry == null) return false;
        lock (entry)
        {
            if (entry.Failures < MaxFailures) return false;
            retryAfter = entry.ResetAt - DateTimeOffset.UtcNow;
            return retryAfter > TimeSpan.Zero;
        }
    }

    public void RecordFailure(string ip)
    {
        var entry = _cache.GetOrCreate(Key(ip), e =>
        {
            var resetAt = DateTimeOffset.UtcNow.Add(Window);
            e.AbsoluteExpiration = resetAt;
            return new Entry { ResetAt = resetAt };
        })!;
        lock (entry) entry.Failures++;
    }

    public void Reset(string ip) => _cache.Remove(Key(ip));
}
