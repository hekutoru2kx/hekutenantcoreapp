using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
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

// Narrowly scoped to GetAllTenantsAsync (the new unpaged export method) — TenantRepository has
// no pre-existing test file to extend, and backfilling coverage for its other, unrelated
// pre-existing methods is out of scope here.
public class TenantRepositoryTests
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

    private static TenantRepository CreateRepository(HekutenantcoreappDbContext context, int? exportMaxRows = null) =>
        new(context, Mock.Of<IStringLocalizer<Messages>>(), new ExportSettings(exportMaxRows));

    [Fact]
    public async Task GetAllTenantsAsync_Returns_Every_Matching_Row_Unpaged()
    {
        const string dbName = nameof(GetAllTenantsAsync_Returns_Every_Matching_Row_Unpaged);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Tenants.AddRange(
                new Tenant { Name = "Clinic A", TenantType = TenantType.Standard, IsActive = true },
                new Tenant { Name = "Clinic B", TenantType = TenantType.Standard, IsActive = true },
                new Tenant { Name = "Clinic C", TenantType = TenantType.Standard, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context);

        var result = await repository.GetAllTenantsAsync(search: null, sortBy: "name", sortDirection: "desc");

        Assert.Equal(["Clinic C", "Clinic B", "Clinic A"], result.Select(t => t.Name));
    }

    [Fact]
    public async Task GetAllTenantsAsync_Filters_By_Search()
    {
        const string dbName = nameof(GetAllTenantsAsync_Filters_By_Search);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Tenants.AddRange(
                new Tenant { Name = "Northside Clinic", TenantType = TenantType.Standard, IsActive = true },
                new Tenant { Name = "Southside Co", TenantType = TenantType.Enterprise, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context);

        var result = await repository.GetAllTenantsAsync(search: "north", sortBy: null, sortDirection: null);

        Assert.Equal("Northside Clinic", Assert.Single(result).Name);
    }

    [Fact]
    public async Task GetAllTenantsAsync_Throws_When_Result_Exceeds_Cap()
    {
        const string dbName = nameof(GetAllTenantsAsync_Throws_When_Result_Exceeds_Cap);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Tenants.AddRange(
                new Tenant { Name = "Clinic A", TenantType = TenantType.Standard, IsActive = true },
                new Tenant { Name = "Clinic B", TenantType = TenantType.Standard, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context, exportMaxRows: 1);

        await Assert.ThrowsAsync<Exception>(() => repository.GetAllTenantsAsync(null, null, null));
    }

    [Fact]
    public async Task GetAllTenantsAsync_Does_Not_Throw_When_Cap_Is_Null()
    {
        const string dbName = nameof(GetAllTenantsAsync_Does_Not_Throw_When_Cap_Is_Null);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Tenants.AddRange(
                new Tenant { Name = "Clinic A", TenantType = TenantType.Standard, IsActive = true },
                new Tenant { Name = "Clinic B", TenantType = TenantType.Standard, IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context, exportMaxRows: null);

        var result = await repository.GetAllTenantsAsync(null, null, null);
        Assert.Equal(2, result.Count);
    }
}
