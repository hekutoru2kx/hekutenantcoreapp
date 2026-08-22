using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hekutenantcoreapp.Infrastructure.BackgroundServices;

// Pings the database with a trivial SELECT 1 on a configurable schedule to prevent the
// Azure SQL serverless auto-pause from kicking in during business hours. Disabled by default —
// see DatabaseKeepAliveSettings (a singleton config row, not tenant-scoped).
public class DatabaseKeepAliveHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseKeepAliveHostedService> _logger;

    public DatabaseKeepAliveHostedService(IServiceScopeFactory scopeFactory, ILogger<DatabaseKeepAliveHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database keep-alive tick failed");
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HekutenantcoreappDbContext>();

        var settings = await db.DatabaseKeepAliveSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null || !settings.IsEnabled) return;

        var localNow = DateTime.UtcNow.AddHours(settings.GmtOffsetHours);
        if (!IsWithinActiveWindow(localNow.TimeOfDay, settings.ActiveStartTime, settings.ActiveEndTime)) return;

        var intervalMinutes = settings.IntervalUnit == Domain.Enums.KeepAliveIntervalUnit.Hours
            ? settings.IntervalAmount * 60
            : settings.IntervalAmount;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        if (settings.LastPingAt is not null && DateTime.UtcNow - settings.LastPingAt < interval) return;

        await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

        settings.LastPingAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsWithinActiveWindow(TimeSpan now, TimeSpan start, TimeSpan end) =>
        start <= end ? now >= start && now <= end : now >= start || now <= end;
}
