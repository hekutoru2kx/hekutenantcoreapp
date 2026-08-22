using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Data;

// Ensures the single DatabaseKeepAliveSettings row (Id = 1) exists, seeded disabled with the
// documented defaults — the admin page only ever reads/updates this one row, never creates it.
public static class DatabaseKeepAliveSettingsSeeder
{
    public static async Task SeedAsync(HekutenantcoreappDbContext db)
    {
        var exists = await db.DatabaseKeepAliveSettings.AnyAsync();
        if (exists) return;

        db.DatabaseKeepAliveSettings.Add(new Domain.Entities.DatabaseKeepAliveSettings());
        await db.SaveChangesAsync();
    }
}
