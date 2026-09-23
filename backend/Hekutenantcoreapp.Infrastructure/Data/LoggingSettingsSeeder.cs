using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Data;

// Ensures the six LoggingCategorySettings rows (one per LogCategory) and the single
// LoggingRetentionSettings row exist, seeded with sensible defaults — the admin page only ever
// reads/updates these rows, never creates or deletes them. Additive: a LogCategory added later
// would need a matching addition here, same convention as every other seed-once-additive catalog.
public static class LoggingSettingsSeeder
{
    private static readonly Dictionary<LogCategory, LogLevel> DefaultMinimumLevels = new()
    {
        [LogCategory.Http] = LogLevel.Information,
        [LogCategory.Database] = LogLevel.Warning,
        [LogCategory.Security] = LogLevel.Information,
        [LogCategory.Integration] = LogLevel.Warning,
        [LogCategory.Business] = LogLevel.Information,
        [LogCategory.System] = LogLevel.Information
    };

    public static async Task SeedAsync(HekutenantcoreappDbContext db)
    {
        if (!await db.LoggingCategorySettings.AnyAsync())
        {
            foreach (var (category, level) in DefaultMinimumLevels)
                db.LoggingCategorySettings.Add(new LoggingCategorySettings { Category = category, MinimumLevel = level });
        }

        if (!await db.LoggingRetentionSettings.AnyAsync())
            db.LoggingRetentionSettings.Add(new LoggingRetentionSettings());

        await db.SaveChangesAsync();
    }
}
