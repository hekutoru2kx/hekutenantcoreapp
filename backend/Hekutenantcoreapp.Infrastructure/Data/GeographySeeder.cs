namespace Hekutenantcoreapp.Infrastructure.Data;

// Reference geography — the dr5hn Countries-States-Cities database (ODbL v1.0), shipped as
// gzipped CSV under Resources/Geography. Loaded (countries -> states -> cities, FK order)
// only into whichever of the three tables is empty; a no-op once populated.
public static class GeographySeeder
{
    public static Task SeedAsync(HekutenantcoreappDbContext db) => CsvSeeder.SeedAsync(
        db, "Geography",
        ("countries", "id, name, iso2, iso3, phone_code, capital, currency, region, subregion"),
        ("states", "id, name, state_code, country_id"),
        ("cities", "id, name, state_id, country_id"));
}
