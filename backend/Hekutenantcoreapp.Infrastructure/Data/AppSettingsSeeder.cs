using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Data;

// Ensures the single AppSettings row (Id = 1) exists, seeded with defaults — the admin page
// only ever reads/updates this one row, never creates it.
public static class AppSettingsSeeder
{
    public static async Task SeedAsync(HekutenantcoreappDbContext db)
    {
        if (await db.AppSettings.AnyAsync()) return;

        db.AppSettings.Add(new Domain.Entities.AppSettings());
        await db.SaveChangesAsync();
    }
}
