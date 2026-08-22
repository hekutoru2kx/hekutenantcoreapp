using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

// Narrowly scoped to GetAllPersonsAsync (the new unpaged export method) — PersonRepository has
// no pre-existing test file to extend, and backfilling coverage for its other, unrelated
// pre-existing methods is out of scope here.
public class PersonRepositoryTests
{
    private static HekutenantcoreappDbContext CreateContext(string dbName, int tenantId = 1)
    {
        var options = new DbContextOptionsBuilder<HekutenantcoreappDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, "seed-user"), new Claim("tenant_id", tenantId.ToString()) },
                    "TestAuth"))
            }
        };

        return new HekutenantcoreappDbContext(options, accessor);
    }

    private static PersonRepository CreateRepository(HekutenantcoreappDbContext context, int? exportMaxRows = null) =>
        new(context, Mock.Of<IStringLocalizer<Messages>>(), new ExportSettings(exportMaxRows));

    [Fact]
    public async Task GetAllPersonsAsync_Returns_Every_Matching_Row_Unpaged()
    {
        const string dbName = nameof(GetAllPersonsAsync_Returns_Every_Matching_Row_Unpaged);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Persons.AddRange(
                new Person { FirstName = "Alice", LastName = "Anders" },
                new Person { FirstName = "Bob", LastName = "Baker" },
                new Person { FirstName = "Carla", LastName = "Cruz" });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context);

        var result = await repository.GetAllPersonsAsync(search: null, sortBy: "lastname", sortDirection: "desc", countryId: null);

        Assert.Equal(["Cruz", "Baker", "Anders"], result.Select(p => p.LastName));
    }

    [Fact]
    public async Task GetAllPersonsAsync_Throws_When_Result_Exceeds_Cap()
    {
        const string dbName = nameof(GetAllPersonsAsync_Throws_When_Result_Exceeds_Cap);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Persons.AddRange(
                new Person { FirstName = "Alice", LastName = "Anders" },
                new Person { FirstName = "Bob", LastName = "Baker" });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context, exportMaxRows: 1);

        await Assert.ThrowsAsync<Exception>(() => repository.GetAllPersonsAsync(null, null, null, null));
    }
}
