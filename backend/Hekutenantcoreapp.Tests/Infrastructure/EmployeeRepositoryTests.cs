using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Identity;
using Hekutenantcoreapp.Infrastructure.Repositories;
using Hekutenantcoreapp.Application.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

public class EmployeeRepositoryTests
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

    private static EmployeeRepository CreateRepository(HekutenantcoreappDbContext context, int? exportMaxRows = null)
    {
        var userManager = new UserManager<ApplicationUser>(
            new UserStore<ApplicationUser>(context), null!, new PasswordHasher<ApplicationUser>(),
            [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, null!);

        return new EmployeeRepository(context, userManager, Mock.Of<IStringLocalizer<Messages>>(), new ExportSettings(exportMaxRows));
    }

    [Fact]
    public async Task GetEmployeesAsync_Search_Filters_By_UserName_Email_And_JobTitle()
    {
        const string dbName = nameof(GetEmployeesAsync_Search_Filters_By_UserName_Email_And_JobTitle);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var alice = new ApplicationUser { Id = "user-alice", UserName = "alice", Email = "alice@example.com" };
            var bob = new ApplicationUser { Id = "user-bob", UserName = "bob", Email = "bob@example.com" };
            seedContext.Users.AddRange(alice, bob);

            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-alice", JobTitle = "Nurse", IsActive = true });
            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-bob", JobTitle = "Receptionist", IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = CreateRepository(context);

        var byUserName = await repository.GetEmployeesAsync(new EmployeeListQuery { Search = "alice" });
        Assert.Equal(1, byUserName.TotalCount);
        Assert.Equal("Nurse", Assert.Single(byUserName.Items).JobTitle);

        var byEmail = await repository.GetEmployeesAsync(new EmployeeListQuery { Search = "bob@example.com" });
        Assert.Equal(1, byEmail.TotalCount);
        Assert.Equal("Receptionist", Assert.Single(byEmail.Items).JobTitle);

        var byJobTitle = await repository.GetEmployeesAsync(new EmployeeListQuery { Search = "receptionist" });
        Assert.Equal(1, byJobTitle.TotalCount);
        Assert.Equal("user-bob", Assert.Single(byJobTitle.Items).UserId);

        var noSearch = await repository.GetEmployeesAsync(new EmployeeListQuery());
        Assert.Equal(2, noSearch.TotalCount);

        var noMatch = await repository.GetEmployeesAsync(new EmployeeListQuery { Search = "nonexistent" });
        Assert.Equal(0, noMatch.TotalCount);
    }

    [Fact]
    public async Task GetAllEmployeesAsync_Returns_Every_Matching_Row_Unpaged()
    {
        const string dbName = nameof(GetAllEmployeesAsync_Returns_Every_Matching_Row_Unpaged);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var alice = new ApplicationUser { Id = "user-alice", UserName = "alice", Email = "alice@example.com" };
            var bob = new ApplicationUser { Id = "user-bob", UserName = "bob", Email = "bob@example.com" };
            seedContext.Users.AddRange(alice, bob);

            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-alice", JobTitle = "Nurse", IsActive = true });
            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-bob", JobTitle = "Receptionist", IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = CreateRepository(context);

        var result = await repository.GetAllEmployeesAsync(search: null, sortBy: "jobtitle", sortDirection: "asc");

        Assert.Equal(2, result.Count);
        Assert.Equal(["Nurse", "Receptionist"], result.Select(e => e.JobTitle));
    }

    [Fact]
    public async Task GetAllEmployeesAsync_Throws_When_Result_Exceeds_Cap()
    {
        const string dbName = nameof(GetAllEmployeesAsync_Throws_When_Result_Exceeds_Cap);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var alice = new ApplicationUser { Id = "user-alice", UserName = "alice", Email = "alice@example.com" };
            var bob = new ApplicationUser { Id = "user-bob", UserName = "bob", Email = "bob@example.com" };
            seedContext.Users.AddRange(alice, bob);

            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-alice", JobTitle = "Nurse", IsActive = true });
            seedContext.Employees.Add(new Employee { TenantId = 1, UserId = "user-bob", JobTitle = "Receptionist", IsActive = true });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = CreateRepository(context, exportMaxRows: 1);

        await Assert.ThrowsAsync<Exception>(() => repository.GetAllEmployeesAsync(null, null, null));
    }
}
