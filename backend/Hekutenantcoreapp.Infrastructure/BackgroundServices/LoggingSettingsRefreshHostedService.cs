using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Hekutenantcoreapp.Infrastructure.BackgroundServices;

// Keeps the live CategoryLevelSwitches in sync with the SuperAdmin-editable LoggingCategorySettings
// rows — a fixed PeriodicTimer tick — so a change takes effect without a restart. Runs the daily
// system_logs retention sweep off the same timer rather than a second
// hosted service; system_logs isn't part of the EF model (see the CreateSystemLogsTable migration),
// so the sweep is a raw DELETE, same as SystemLogPostgresSink's writes.
public class LoggingSettingsRefreshHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetentionSweepInterval = TimeSpan.FromDays(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CategoryLevelSwitches _switches;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoggingSettingsRefreshHostedService> _logger;
    private DateTime _lastRetentionSweepAt = DateTime.MinValue;

    public LoggingSettingsRefreshHostedService(
        IServiceScopeFactory scopeFactory,
        CategoryLevelSwitches switches,
        IConfiguration configuration,
        ILogger<LoggingSettingsRefreshHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _switches = switches;
        _configuration = configuration;
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
                _logger.LogError(ex, "Logging settings refresh tick failed");
            }
        }
    }

    private async Task TickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HekutenantcoreappDbContext>();

        var categories = await db.LoggingCategorySettings.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var category in categories)
            _switches.SetMinimumLevel(category.Category, category.MinimumLevel);

        if (DateTime.UtcNow - _lastRetentionSweepAt < RetentionSweepInterval) return;
        _lastRetentionSweepAt = DateTime.UtcNow;

        var retention = await db.LoggingRetentionSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (retention is null) return;

        var cutoff = DateTime.UtcNow.AddDays(-retention.RetentionDays);
        await using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM system_logs WHERE timestamp < @cutoff";
        cmd.Parameters.AddWithValue("cutoff", cutoff);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
