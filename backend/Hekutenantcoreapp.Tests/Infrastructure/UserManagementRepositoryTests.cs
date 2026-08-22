using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

// Narrowly scoped to GetAllUsersAsync (the new unpaged export method) — UserManagementRepository
// has no pre-existing test file to extend, and backfilling coverage for its other, unrelated
// pre-existing methods (including the RoleFilter/TotalCount quirk in GetUsersAsync) is out of
// scope here.
public class UserManagementRepositoryTests
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

    private static UserManagementRepository CreateRepository(HekutenantcoreappDbContext context, int? exportMaxRows = null)
    {
        var userManager = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser>(context), null!, new PasswordHasher<ApplicationUser>(),
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, null!);

        return new UserManagementRepository(userManager, Mock.Of<IStringLocalizer<Messages>>(), context, new ExportSettings(exportMaxRows));
    }

    [Fact]
    public async Task GetAllUsersAsync_Returns_Every_Matching_Row_Unpaged()
    {
        const string dbName = nameof(GetAllUsersAsync_Returns_Every_Matching_Row_Unpaged);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { UserName = "alice", Email = "alice@example.com", IsActive = true },
                new ApplicationUser { UserName = "bob", Email = "bob@example.com", IsActive = false });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context);

        var result = await repository.GetAllUsersAsync(search: null, sortBy: "username", sortDirection: "asc", roleFilter: null, statusFilter: null);

        Assert.Equal(["alice", "bob"], result.Select(u => u.UserName));
    }

    [Fact]
    public async Task GetAllUsersAsync_Filters_By_StatusFilter()
    {
        const string dbName = nameof(GetAllUsersAsync_Filters_By_StatusFilter);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { UserName = "alice", Email = "alice@example.com", IsActive = true },
                new ApplicationUser { UserName = "bob", Email = "bob@example.com", IsActive = false });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context);

        var result = await repository.GetAllUsersAsync(null, null, null, null, statusFilter: true);

        Assert.Equal("alice", Assert.Single(result).UserName);
    }

    [Fact]
    public async Task GetAllUsersAsync_Throws_When_Result_Exceeds_Cap()
    {
        const string dbName = nameof(GetAllUsersAsync_Throws_When_Result_Exceeds_Cap);
        await using (var seedContext = CreateContext(dbName))
        {
            seedContext.Users.AddRange(
                new ApplicationUser { UserName = "alice", Email = "alice@example.com", IsActive = true },
                new ApplicationUser { UserName = "bob", Email = "bob@example.com", IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName);
        var repository = CreateRepository(context, exportMaxRows: 1);

        await Assert.ThrowsAsync<Exception>(() => repository.GetAllUsersAsync(null, null, null, null, null));
    }
}
