using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

// Reads/updates the single DatabaseKeepAliveSettings row — DatabaseKeepAliveSettingsSeeder
// guarantees it exists at startup, so GetSettingsAsync never returns null in practice.
public class DatabaseKeepAliveRepository : IDatabaseKeepAliveRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly IStringLocalizer<Messages> _localizer;

    public DatabaseKeepAliveRepository(HekutenantcoreappDbContext context, IStringLocalizer<Messages> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<DatabaseKeepAliveSettingsResult> GetSettingsAsync()
    {
        var settings = await _context.DatabaseKeepAliveSettings.FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["DatabaseKeepAliveSettingsNotFound"]);

        return MapToResult(settings);
    }

    public async Task UpdateSettingsAsync(UpdateDatabaseKeepAliveSettingsRequest request)
    {
        var settings = await _context.DatabaseKeepAliveSettings.FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["DatabaseKeepAliveSettingsNotFound"]);

        settings.IsEnabled = request.IsEnabled;
        settings.IntervalAmount = request.IntervalAmount;
        settings.IntervalUnit = Enum.Parse<KeepAliveIntervalUnit>(request.IntervalUnit);
        settings.GmtOffsetHours = request.GmtOffsetHours;
        settings.ActiveStartTime = TimeSpan.Parse(request.ActiveStartTime);
        settings.ActiveEndTime = TimeSpan.Parse(request.ActiveEndTime);

        await _context.SaveChangesAsync();
    }

    private static DatabaseKeepAliveSettingsResult MapToResult(Domain.Entities.DatabaseKeepAliveSettings settings) => new()
    {
        IsEnabled = settings.IsEnabled,
        IntervalAmount = settings.IntervalAmount,
        IntervalUnit = settings.IntervalUnit.ToString(),
        GmtOffsetHours = settings.GmtOffsetHours,
        ActiveStartTime = settings.ActiveStartTime.ToString(@"hh\:mm"),
        ActiveEndTime = settings.ActiveEndTime.ToString(@"hh\:mm"),
        LastPingAt = settings.LastPingAt
    };
}
