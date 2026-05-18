using GestionCo.Api.Application.Common.Interfaces;
using GestionCo.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionCo.Api.Infrastructure.Services;

public class VenteEcheanceJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VenteEcheanceJob> _logger;

    public VenteEcheanceJob(IServiceScopeFactory scopeFactory, ILogger<VenteEcheanceJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VenteEcheanceJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextMidnightUtc();
            _logger.LogInformation("VenteEcheanceJob: next run in {Delay:hh\\:mm\\:ss}", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunAsync(stoppingToken);
        }

        _logger.LogInformation("VenteEcheanceJob stopped.");
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db    = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today    = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            // Ventes échues non soldées
            var ventesEchues = await db.Ventes
                .Include(v => v.Client)
                .Where(v => v.DateEcheance != null
                         && v.DateEcheance < tomorrow
                         && v.MontantPaye < v.MontantTotal
                         && v.Statut != StatutVente.Annule
                         && v.Statut != StatutVente.Paye)
                .ToListAsync(ct);

            int count = 0;
            foreach (var vente in ventesEchues)
            {
                // Une seule notification par vente par jour
                var dejaNotifie = await db.Notifications
                    .AnyAsync(n => n.EntiteId == vente.Id
                                && n.Categorie == CategorieNotification.Vente
                                && n.CreatedAt >= today
                                && n.CreatedAt < tomorrow, ct);
                if (dejaNotifie) continue;

                var reste       = vente.MontantTotal - vente.MontantPaye;
                var echeance    = vente.DateEcheance!.Value.Date;
                var isToday     = echeance == today;
                var joursRetard = (today - echeance).Days;
                var nomClient   = vente.Client?.NomClient ?? "Client inconnu";

                var titre   = isToday
                    ? "Échéance de paiement aujourd'hui"
                    : $"Paiement en retard — {joursRetard} jour(s)";

                var message = isToday
                    ? $"{nomClient} · {vente.Reference} — {reste:N2} MAD à encaisser aujourd'hui"
                    : $"{nomClient} · {vente.Reference} — {reste:N2} MAD dû depuis le {echeance:dd/MM/yyyy}";

                await notif.CreateAsync(
                    titre,
                    message,
                    isToday ? TypeNotification.Warning : TypeNotification.Danger,
                    CategorieNotification.Vente,
                    vente.Id,
                    vente.Reference,
                    $"/ventes/{vente.Id}",
                    ct);

                count++;
            }

            if (count > 0)
                _logger.LogInformation("VenteEcheanceJob: {Count} notification(s) d'échéance créée(s).", count);
            else
                _logger.LogInformation("VenteEcheanceJob: aucune échéance à notifier.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VenteEcheanceJob: erreur lors de la vérification des échéances.");
        }
    }

    private static TimeSpan TimeUntilNextMidnightUtc()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1);
        var delay = nextMidnight - now;
        return delay < TimeSpan.FromSeconds(1) ? delay.Add(TimeSpan.FromDays(1)) : delay;
    }
}
