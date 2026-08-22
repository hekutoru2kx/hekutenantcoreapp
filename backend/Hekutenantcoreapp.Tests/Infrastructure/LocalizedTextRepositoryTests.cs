using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

public class LocalizedTextRepositoryTests
{
    private static HekutenantcoreappDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HekutenantcoreappDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "seed-user") }, "TestAuth"))
            }
        };

        return new HekutenantcoreappDbContext(options, accessor);
    }

    [Fact]
    public async Task ReplaceTranslationsAsync_Inserts_New_Translations()
    {
        const string dbName = nameof(ReplaceTranslationsAsync_Inserts_New_Translations);
        await using var context = CreateContext(dbName);
        var repository = new LocalizedTextRepository(context);

        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget" });

        var result = await repository.GetTranslationsAsync("Product", new[] { 1 }, "Name");
        Assert.Equal("Widget", result[1]["es"]);
    }

    [Fact]
    public async Task ReplaceTranslationsAsync_Updates_Existing_Value_For_Same_Language()
    {
        const string dbName = nameof(ReplaceTranslationsAsync_Updates_Existing_Value_For_Same_Language);
        await using var context = CreateContext(dbName);
        var repository = new LocalizedTextRepository(context);

        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget" });
        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget (corregido)" });

        var result = await repository.GetTranslationsAsync("Product", new[] { 1 }, "Name");
        Assert.Equal("Widget (corregido)", result[1]["es"]);
        Assert.Single(await context.LocalizedTexts.ToListAsync());
    }

    [Fact]
    public async Task ReplaceTranslationsAsync_Removes_Language_Not_In_New_Set()
    {
        const string dbName = nameof(ReplaceTranslationsAsync_Removes_Language_Not_In_New_Set);
        await using var context = CreateContext(dbName);
        var repository = new LocalizedTextRepository(context);

        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget", ["fr"] = "Gadget FR" });
        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget" });

        var result = await repository.GetTranslationsAsync("Product", new[] { 1 }, "Name");
        Assert.Single(result[1]);
        Assert.True(result[1].ContainsKey("es"));
        Assert.False(result[1].ContainsKey("fr"));
    }

    [Fact]
    public async Task GetTranslationsAsync_Batches_Multiple_Entity_Ids_And_Omits_Entities_With_None()
    {
        const string dbName = nameof(GetTranslationsAsync_Batches_Multiple_Entity_Ids_And_Omits_Entities_With_None);
        await using var context = CreateContext(dbName);
        var repository = new LocalizedTextRepository(context);

        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget" });
        await repository.ReplaceTranslationsAsync("Product", 2, "Name", new Dictionary<string, string> { ["es"] = "Gadget" });

        var result = await repository.GetTranslationsAsync("Product", new[] { 1, 2, 3 }, "Name");

        Assert.Equal(2, result.Count);
        Assert.Equal("Widget", result[1]["es"]);
        Assert.Equal("Gadget", result[2]["es"]);
        Assert.False(result.ContainsKey(3));
    }

    [Fact]
    public async Task GetTranslationsAsync_Does_Not_Cross_Contaminate_Between_Fields_Or_Entity_Names()
    {
        const string dbName = nameof(GetTranslationsAsync_Does_Not_Cross_Contaminate_Between_Fields_Or_Entity_Names);
        await using var context = CreateContext(dbName);
        var repository = new LocalizedTextRepository(context);

        await repository.ReplaceTranslationsAsync("Product", 1, "Name", new Dictionary<string, string> { ["es"] = "Widget" });
        await repository.ReplaceTranslationsAsync("Product", 1, "Description", new Dictionary<string, string> { ["es"] = "Descripcion" });
        await repository.ReplaceTranslationsAsync("ProductVariant", 1, "Name", new Dictionary<string, string> { ["es"] = "Deberia no aparecer" });

        var nameTranslations = await repository.GetTranslationsAsync("Product", new[] { 1 }, "Name");

        Assert.Single(nameTranslations[1]);
        Assert.Equal("Widget", nameTranslations[1]["es"]);
    }
}
