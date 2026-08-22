using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Data;

// Ensures the single MultiTenantSettings row (Id = 1) exists, seeded with both flags off and
// no default tenant — the admin page only ever reads/updates this one row, never creates it.
public static class MultiTenantSettingsSeeder
{
    public static async Task SeedAsync(HekutenantcoreappDbContext db)
    {
        var exists = await db.MultiTenantSettings.AnyAsync();
        if (exists) return;

        db.MultiTenantSettings.Add(new Domain.Entities.MultiTenantSettings());
        await db.SaveChangesAsync();
    }
}
