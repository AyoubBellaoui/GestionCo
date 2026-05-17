using GestionCo.Api.Application.Charges;
using GestionCo.Api.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GestionCo.Api.Infrastructure.Services;

public class RecurringChargesJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringChargesJob> _logger;

    public RecurringChargesJob(IServiceScopeFactory scopeFactory, ILogger<RecurringChargesJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecurringChargesJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = TimeUntilNextMidnightUtc();
            _logger.LogInformation("RecurringChargesJob: next run in {Delay:hh\\:mm\\:ss} (at {Next:u})", delay, DateTime.UtcNow.Add(delay));

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunAsync(stoppingToken);
        }

        _logger.LogInformation("RecurringChargesJob stopped.");
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Use the first active admin as the system user for audit logs
            var systemUserId = await db.Utilisateurs
                .Where(u => u.IsActive && u.Role == Domain.Enums.RoleUtilisateur.Admin)
                .OrderBy(u => u.Id)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (systemUserId is null)
            {
                _logger.LogWarning("RecurringChargesJob: no active admin found, skipping run.");
                return;
            }

            var count = await mediator.Send(new GenererChargesRecurrentesCommand(systemUserId), ct);
            _logger.LogInformation("RecurringChargesJob: {Count} recurring charge(s) generated.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecurringChargesJob: error during execution.");
        }
    }

    private static TimeSpan TimeUntilNextMidnightUtc()
    {
        var now = DateTime.UtcNow;
        var nextMidnight = now.Date.AddDays(1); // tomorrow 00:00:00 UTC
        var delay = nextMidnight - now;
        // Guard: if delay is tiny (< 1s) due to clock jitter, add a full day
        return delay < TimeSpan.FromSeconds(1) ? delay.Add(TimeSpan.FromDays(1)) : delay;
    }
}
