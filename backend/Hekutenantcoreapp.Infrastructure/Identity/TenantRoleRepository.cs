using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class TenantRoleRepository : ITenantRoleRepository
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public TenantRoleRepository(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IList<string>> GetAssignableRoleNamesAsync() =>
        await _roleManager.Roles
            .Where(r => r.Name != "SuperAdmin")
            .Select(r => r.Name!)
            .ToListAsync();

    public async Task<IList<TenantRoleResult>> GetVisibleRolesAsync()
    {
        var roles = await _roleManager.Roles
            .Where(r => r.Name != "SuperAdmin")
            .ToListAsync();

        var result = new List<TenantRoleResult>();
        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            result.Add(new TenantRoleResult
            {
                Name = role.Name!,
                Claims = claims.Select(c => new PermissionClaimResult { Module = c.Type, Action = c.Value }).ToList()
            });
        }

        return result;
    }
}
