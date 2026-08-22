using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

public class TenantIsolationTests
{
    private static HekutenantcoreappDbContext CreateContext(string dbName, int? tenantId, bool withHttpContext = true)
    {
        var options = new DbContextOptionsBuilder<HekutenantcoreappDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = new HttpContextAccessor();
        if (withHttpContext)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "test-user") };
            if (tenantId.HasValue)
                claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            };
        }

        return new HekutenantcoreappDbContext(options, accessor);
    }

    [Fact]
    public async Task Persons_Are_Scoped_To_Current_Tenant()
    {
        const string dbName = nameof(Persons_Are_Scoped_To_Current_Tenant);

        int personAId, personBId;
        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var personA = new Person { TenantId = 1, FirstName = "A", LastName = "Tenant" };
            var personB = new Person { TenantId = 2, FirstName = "B", LastName = "Tenant" };
            seedContext.Persons.Add(personA);
            seedContext.Persons.Add(personB);
            await seedContext.SaveChangesAsync();
            personAId = personA.Id;
            personBId = personB.Id;
        }

        await using var tenantAContext = CreateContext(dbName, tenantId: 1);
        var visiblePersons = await tenantAContext.Persons.ToListAsync();

        Assert.Single(visiblePersons);
        Assert.Equal(personAId, visiblePersons[0].Id);
        Assert.Null(await tenantAContext.Persons.FindAsync(personBId));
    }

    [Fact]
    public async Task Persons_Are_Invisible_Without_A_Resolvable_Tenant()
    {
        const string dbName = nameof(Persons_Are_Invisible_Without_A_Resolvable_Tenant);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            seedContext.Persons.Add(new Person { TenantId = 1, FirstName = "A", LastName = "Tenant" });
            await seedContext.SaveChangesAsync();
        }

        await using var noClaimContext = CreateContext(dbName, tenantId: null);
        Assert.Empty(await noClaimContext.Persons.ToListAsync());

        await using var noHttpContext = CreateContext(dbName, tenantId: null, withHttpContext: false);
        Assert.Empty(await noHttpContext.Persons.ToListAsync());
    }

    [Fact]
    public async Task SaveChanges_AutoStamps_TenantId_From_Current_Claim()
    {
        const string dbName = nameof(SaveChanges_AutoStamps_TenantId_From_Current_Claim);

        await using var context = CreateContext(dbName, tenantId: 7);
        var person = new Person { FirstName = "Auto", LastName = "Stamped" };
        context.Persons.Add(person);
        await context.SaveChangesAsync();

        Assert.Equal(7, person.TenantId);
    }

    [Fact]
    public async Task SaveChanges_Throws_When_No_Tenant_Context_Is_Resolvable()
    {
        const string dbName = nameof(SaveChanges_Throws_When_No_Tenant_Context_Is_Resolvable);

        await using var context = CreateContext(dbName, tenantId: null);
        context.Persons.Add(new Person { FirstName = "No", LastName = "Tenant" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_Respects_Explicitly_Set_TenantId_During_Seeding()
    {
        const string dbName = nameof(SaveChanges_Respects_Explicitly_Set_TenantId_During_Seeding);

        // Simulates Program.cs bootstrap seeding: no HttpContext at all, TenantId set explicitly.
        await using var context = CreateContext(dbName, tenantId: null, withHttpContext: false);
        var person = new Person { TenantId = 3, FirstName = "Seeded", LastName = "Tenant" };
        context.Persons.Add(person);
        await context.SaveChangesAsync();

        Assert.Equal(3, person.TenantId);
    }
}
