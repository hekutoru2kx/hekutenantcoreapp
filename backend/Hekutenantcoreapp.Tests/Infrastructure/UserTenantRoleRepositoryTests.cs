using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure;

public class UserTenantRoleRepositoryTests
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

    // Backed by the same InMemory HekutenantcoreappDbContext (which already has Identity's Roles
    // DbSet via IdentityDbContext<ApplicationUser>) rather than mocked, since RoleManager's
    // IQueryable-returning members aren't friendly to plain in-memory LINQ mocks under EF's
    // async operators.
    private static async Task<RoleManager<IdentityRole>> CreateRoleManagerAsync(HekutenantcoreappDbContext context, params IdentityRole[] roles)
    {
        var normalizer = new UpperInvariantLookupNormalizer();
        var store = new RoleStore<IdentityRole>(context);
        var roleManager = new RoleManager<IdentityRole>(store, [], normalizer, new IdentityErrorDescriber(), null!);

        foreach (var role in roles)
        {
            role.NormalizedName = normalizer.NormalizeName(role.Name);
            await roleManager.CreateAsync(role);
        }

        return roleManager;
    }

    [Fact]
    public async Task GetRoleNamesAsync_Excludes_Expired_Assignments()
    {
        const string dbName = nameof(GetRoleNamesAsync_Excludes_Expired_Assignments);
        IdentityRole nurseRole, adminRole;

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var seedRoleManager = await CreateRoleManagerAsync(seedContext,
                new IdentityRole("Nurse") { Id = "role-nurse" }, new IdentityRole("Admin") { Id = "role-admin" });
            nurseRole = await seedRoleManager.FindByIdAsync("role-nurse");
            adminRole = await seedRoleManager.FindByIdAsync("role-admin");

            seedContext.UserTenantRoles.Add(new UserTenantRole
            {
                TenantId = 1, UserId = "user-1", RoleId = nurseRole.Id, ExpiresAt = DateTime.UtcNow.AddDays(-1)
            });
            seedContext.UserTenantRoles.Add(new UserTenantRole
            {
                TenantId = 1, UserId = "user-1", RoleId = adminRole.Id, ExpiresAt = null
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        var activeRoles = await repository.GetRoleNamesAsync("user-1", 1);

        Assert.DoesNotContain("Nurse", activeRoles);
        Assert.Contains("Admin", activeRoles);
    }

    [Fact]
    public async Task GetRoleAssignmentsAsync_Returns_Expired_Rows_Flagged()
    {
        const string dbName = nameof(GetRoleAssignmentsAsync_Returns_Expired_Rows_Flagged);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            await CreateRoleManagerAsync(seedContext, new IdentityRole("Nurse") { Id = "role-nurse" });

            seedContext.UserTenantRoles.Add(new UserTenantRole
            {
                TenantId = 1, UserId = "user-1", RoleId = "role-nurse", ExpiresAt = DateTime.UtcNow.AddDays(-1)
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        var assignments = await repository.GetRoleAssignmentsAsync("user-1", 1);

        var nurseAssignment = Assert.Single(assignments);
        Assert.Equal("Nurse", nurseAssignment.RoleName);
        Assert.True(nurseAssignment.IsExpired);
    }

    [Fact]
    public async Task AssignRolesAsync_Replaces_Existing_Assignments_With_New_Expiries()
    {
        const string dbName = nameof(AssignRolesAsync_Replaces_Existing_Assignments_With_New_Expiries);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            await CreateRoleManagerAsync(seedContext,
                new IdentityRole("Nurse") { Id = "role-nurse" }, new IdentityRole("Admin") { Id = "role-admin" });

            seedContext.UserTenantRoles.Add(new UserTenantRole { TenantId = 1, UserId = "user-1", RoleId = "role-nurse" });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        var expiry = DateTime.UtcNow.AddMonths(1);
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest>
        {
            new() { RoleName = "Admin", ExpiresAt = expiry }
        });

        var assignments = await repository.GetRoleAssignmentsAsync("user-1", 1);
        var assignment = Assert.Single(assignments);
        Assert.Equal("Admin", assignment.RoleName);
        Assert.Equal(expiry, assignment.ExpiresAt);
    }

    [Fact]
    public async Task AssignRolesAsync_Reassigning_Same_Role_Preserves_History_Instead_Of_Overwriting()
    {
        const string dbName = nameof(AssignRolesAsync_Reassigning_Same_Role_Preserves_History_Instead_Of_Overwriting);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            await CreateRoleManagerAsync(seedContext, new IdentityRole("Nurse") { Id = "role-nurse" });
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        // First stint (e.g. before being let go), then a fresh grant later (e.g. rehire).
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest> { new() { RoleName = "Nurse" } });
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest>());
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest> { new() { RoleName = "Nurse" } });

        var allRows = await context.UserTenantRoles.IgnoreQueryFilters()
            .Where(r => r.UserId == "user-1" && r.TenantId == 1).ToListAsync();
        Assert.Equal(2, allRows.Count);
        Assert.Single(allRows, r => r.RevokedAt != null);
        Assert.Single(allRows, r => r.RevokedAt == null);

        var current = await repository.GetRoleAssignmentsAsync("user-1", 1);
        Assert.Single(current);
    }

    [Fact]
    public async Task RevokeAllAsync_Closes_Open_Assignments_Without_Blocking_Future_Regrants()
    {
        const string dbName = nameof(RevokeAllAsync_Closes_Open_Assignments_Without_Blocking_Future_Regrants);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var seedRoleManager = await CreateRoleManagerAsync(seedContext, new IdentityRole("Nurse") { Id = "role-nurse" });
            var nurseRole = await seedRoleManager.FindByIdAsync("role-nurse");

            seedContext.UserTenantRoles.Add(new UserTenantRole { TenantId = 1, UserId = "user-1", RoleId = nurseRole.Id });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        await repository.RevokeAllAsync("user-1", 1);

        Assert.Empty(await repository.GetRoleNamesAsync("user-1", 1));
        Assert.Empty(await repository.GetRoleAssignmentsAsync("user-1", 1));

        // A later regrant of the same role must not collide with the now-revoked row.
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest> { new() { RoleName = "Nurse" } });
        Assert.Contains("Nurse", await repository.GetRoleNamesAsync("user-1", 1));
    }

    [Fact]
    public async Task AssignRolesAsync_Editing_An_Held_Role_Updates_In_Place_Preserving_CreatedAt()
    {
        const string dbName = nameof(AssignRolesAsync_Editing_An_Held_Role_Updates_In_Place_Preserving_CreatedAt);

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            await CreateRoleManagerAsync(seedContext, new IdentityRole("Nurse") { Id = "role-nurse" });
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest> { new() { RoleName = "Nurse" } });
        var original = Assert.Single(await context.UserTenantRoles.IgnoreQueryFilters()
            .Where(r => r.UserId == "user-1" && r.TenantId == 1).ToListAsync());
        var originalId = original.Id;
        var originalCreatedAt = original.CreatedAt;

        var newExpiry = DateTime.UtcNow.AddMonths(2);
        await repository.AssignRolesAsync("user-1", 1, new List<RoleAssignmentRequest>
        {
            new() { RoleName = "Nurse", ExpiresAt = newExpiry }
        });

        var allRows = await context.UserTenantRoles.IgnoreQueryFilters()
            .Where(r => r.UserId == "user-1" && r.TenantId == 1).ToListAsync();
        var updated = Assert.Single(allRows);

        Assert.Equal(originalId, updated.Id);
        Assert.Equal(originalCreatedAt, updated.CreatedAt);
        Assert.Equal(newExpiry, updated.ExpiresAt);
        Assert.Null(updated.RevokedAt);
    }

    [Fact]
    public async Task GetRoleNamesAsync_Excludes_Not_Yet_Started_Assignments()
    {
        const string dbName = nameof(GetRoleNamesAsync_Excludes_Not_Yet_Started_Assignments);
        IdentityRole nurseRole, adminRole;

        await using (var seedContext = CreateContext(dbName, tenantId: 1))
        {
            var seedRoleManager = await CreateRoleManagerAsync(seedContext,
                new IdentityRole("Nurse") { Id = "role-nurse" }, new IdentityRole("Admin") { Id = "role-admin" });
            nurseRole = await seedRoleManager.FindByIdAsync("role-nurse");
            adminRole = await seedRoleManager.FindByIdAsync("role-admin");

            seedContext.UserTenantRoles.Add(new UserTenantRole
            {
                TenantId = 1, UserId = "user-1", RoleId = nurseRole.Id, StartsAt = DateTime.UtcNow.AddDays(7)
            });
            seedContext.UserTenantRoles.Add(new UserTenantRole
            {
                TenantId = 1, UserId = "user-1", RoleId = adminRole.Id, StartsAt = DateTime.UtcNow.AddDays(-1)
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateContext(dbName, tenantId: 1);
        var repository = new UserTenantRoleRepository(context, new RoleManager<IdentityRole>(
            new RoleStore<IdentityRole>(context), [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!));

        var activeRoles = await repository.GetRoleNamesAsync("user-1", 1);
        Assert.DoesNotContain("Nurse", activeRoles);
        Assert.Contains("Admin", activeRoles);

        var assignments = await repository.GetRoleAssignmentsAsync("user-1", 1);
        Assert.True(assignments.Single(a => a.RoleName == "Nurse").IsPending);
        Assert.False(assignments.Single(a => a.RoleName == "Admin").IsPending);
    }
}
