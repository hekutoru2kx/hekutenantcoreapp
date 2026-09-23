using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

// Reads/updates the six LoggingCategorySettings rows (one per LogCategory) plus the single
// LoggingRetentionSettings row — LoggingSettingsSeeder guarantees all seven exist at startup.
// Security's MinimumLevel is clamped server-side to never be stricter than Warning: it's the only
// audit trail for auth events, so a SuperAdmin can loosen it (more verbose) but not silence it.
public class LoggingSettingsRepository : ILoggingSettingsRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly IStringLocalizer<Messages> _localizer;

    public LoggingSettingsRepository(HekutenantcoreappDbContext context, IStringLocalizer<Messages> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<LoggingSettingsResult> GetSettingsAsync()
    {
        var categories = await _context.LoggingCategorySettings.OrderBy(c => c.Category).ToListAsync();
        var retention = await _context.LoggingRetentionSettings.FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["LoggingSettingsNotFound"]);

        return new LoggingSettingsResult
        {
            Categories = categories.Select(c => new LoggingCategorySettingResult
            {
                Category = c.Category.ToString(),
                MinimumLevel = c.MinimumLevel.ToString()
            }).ToList(),
            RetentionDays = retention.RetentionDays
        };
    }

    public async Task UpdateSettingsAsync(UpdateLoggingSettingsRequest request)
    {
        var categories = await _context.LoggingCategorySettings.ToListAsync();
        var retention = await _context.LoggingRetentionSettings.FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["LoggingSettingsNotFound"]);

        foreach (var update in request.Categories)
        {
            var category = Enum.Parse<LogCategory>(update.Category);
            var minimumLevel = Enum.Parse<LogLevel>(update.MinimumLevel);

            if (category == LogCategory.Security && minimumLevel > LogLevel.Warning)
                minimumLevel = LogLevel.Warning;

            var row = categories.FirstOrDefault(c => c.Category == category);
            if (row is not null) row.MinimumLevel = minimumLevel;
        }

        retention.RetentionDays = request.RetentionDays;

        await _context.SaveChangesAsync();
    }
}
