using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Hekutenantcoreapp.Infrastructure.Data;

// Keeps every ReportableEnumCatalog lookup table in sync with its enum's own definition —
// adds rows for enum members that don't have one yet, on every startup. Never removes rows
// (a value dropped from the enum still exists historically in old data) and never edits
// application code's enum types; this is purely a one-way reflection of Enum.GetNames().
public static class EnumLookupSeeder
{
    public static async Task SeedAllAsync(HekutenantcoreappDbContext db)
    {
        foreach (var (_, enumType) in ReportableEnumCatalog.Enums)
        {
            var method = typeof(EnumLookupSeeder)
                .GetMethod(nameof(SeedOneAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(enumType);
            await (Task)method.Invoke(null, [db])!;
        }
    }

    private static async Task SeedOneAsync<TEnum>(HekutenantcoreappDbContext db) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var existing = (await db.Set<EnumLookup<TEnum>>().Select(e => e.Code).ToListAsync()).ToHashSet();

        for (var i = 0; i < values.Length; i++)
        {
            if (!existing.Contains(values[i]))
                db.Set<EnumLookup<TEnum>>().Add(new EnumLookup<TEnum> { Code = values[i], SortOrder = i });
        }

        await db.SaveChangesAsync();
    }
}
