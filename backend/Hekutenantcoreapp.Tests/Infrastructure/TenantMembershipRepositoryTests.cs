using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

public class TenantMembershipRepositoryTests
{
    private static HekutenantcoreappDbContext CreateContext(string dbName, int tenantId)
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

    [Fact]
    public async Task SuspendAsync_Blocks_Membership_And_ActivateAsync_Restores_It()
    {
        const string dbName = nameof(SuspendAsync_Blocks_Membership_And_ActivateAsync_Restores_It);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            seedContext.TenantMemberships.Add(new TenantMembership
            {
                TenantId = 1, UserId = "user-1", Status = TenantMembershipStatus.Active
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new TenantMembershipRepository(context);

        Assert.Equal(TenantMembershipStatus.Active, await repository.GetStatusAsync("user-1", 1));
        Assert.True(await repository.HasActiveMembershipAsync("user-1", 1));

        await repository.SuspendAsync("user-1", 1);

        Assert.Equal(TenantMembershipStatus.Suspended, await repository.GetStatusAsync("user-1", 1));
        Assert.False(await repository.HasActiveMembershipAsync("user-1", 1));
        Assert.True(await repository.HasAnyMembershipAsync("user-1", 1));

        await repository.ActivateAsync("user-1", 1);

        Assert.Equal(TenantMembershipStatus.Active, await repository.GetStatusAsync("user-1", 1));
        Assert.True(await repository.HasActiveMembershipAsync("user-1", 1));
    }

    [Fact]
    public async Task GetStatusAsync_Returns_Null_When_No_Membership_Exists()
    {
        const string dbName = nameof(GetStatusAsync_Returns_Null_When_No_Membership_Exists);

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new TenantMembershipRepository(context);

        Assert.Null(await repository.GetStatusAsync("no-such-user", 1));
    }
}
