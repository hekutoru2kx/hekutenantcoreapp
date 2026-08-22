using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class UserTenantRoleRepository : IUserTenantRoleRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserTenantRoleRepository(HekutenantcoreappDbContext context, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<IList<string>> GetRoleNamesAsync(string userId, int tenantId)
    {
        var now = DateTime.UtcNow;
        var roleIds = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.RevokedAt == null
                && (r.StartsAt == null || r.StartsAt <= now)
                && (r.ExpiresAt == null || r.ExpiresAt > now))
            .Select(r => r.RoleId)
            .ToListAsync();

        if (roleIds.Count == 0) return new List<string>();

        return await _roleManager.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name!)
            .ToListAsync();
    }

    public async Task<IList<RoleAssignmentResult>> GetRoleAssignmentsAsync(string userId, int tenantId)
    {
        var assignments = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.RevokedAt == null)
            .ToListAsync();

        if (assignments.Count == 0) return new List<RoleAssignmentResult>();

        var roleIds = assignments.Select(a => a.RoleId).ToList();
        var roleNames = await _roleManager.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name!);

        var userIds = assignments.Select(a => a.CreatedBy)
            .Concat(assignments.Select(a => a.UpdatedBy))
            .Distinct()
            .ToList();
        var userNames = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id);

        var now = DateTime.UtcNow;
        return assignments.Select(a => new RoleAssignmentResult
        {
            RoleName = roleNames.GetValueOrDefault(a.RoleId, a.RoleId),
            StartsAt = a.StartsAt,
            ExpiresAt = a.ExpiresAt,
            IsPending = a.StartsAt != null && a.StartsAt > now,
            IsExpired = a.ExpiresAt != null && a.ExpiresAt <= now,
            CreatedAt = a.CreatedAt,
            CreatedByName = userNames.GetValueOrDefault(a.CreatedBy, a.CreatedBy),
            UpdatedAt = a.UpdatedAt,
            UpdatedByName = userNames.GetValueOrDefault(a.UpdatedBy, a.UpdatedBy)
        }).ToList();
    }

    // Diffs against the currently open assignments rather than blanket revoke-and-recreate:
    // a role that stays assigned just gets its StartsAt/ExpiresAt updated in place (preserving
    // CreatedAt/CreatedBy — it's an edit, not a new grant); a role dropped from the set is
    // soft-revoked; a role not previously held opens a fresh row.
    public async Task AssignRolesAsync(string userId, int tenantId, IList<RoleAssignmentRequest> assignments)
    {
        var now = DateTime.UtcNow;
        var openRows = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.RevokedAt == null)
            .ToListAsync();

        var roleIdByName = new Dictionary<string, string>();
        foreach (var assignment in assignments)
        {
            var role = await _roleManager.FindByNameAsync(assignment.RoleName);
            if (role != null) roleIdByName[assignment.RoleName] = role.Id;
        }

        var desiredRoleIds = roleIdByName.Values.ToHashSet();

        foreach (var row in openRows.Where(r => !desiredRoleIds.Contains(r.RoleId)))
            row.RevokedAt = now;

        foreach (var assignment in assignments)
        {
            if (!roleIdByName.TryGetValue(assignment.RoleName, out var roleId)) continue;

            var existing = openRows.FirstOrDefault(r => r.RoleId == roleId);
            if (existing != null)
            {
                existing.StartsAt = assignment.StartsAt;
                existing.ExpiresAt = assignment.ExpiresAt;
            }
            else
            {
                _context.UserTenantRoles.Add(new UserTenantRole
                {
                    UserId = userId,
                    TenantId = tenantId,
                    RoleId = roleId,
                    StartsAt = assignment.StartsAt,
                    ExpiresAt = assignment.ExpiresAt
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllAsync(string userId, int tenantId)
    {
        var now = DateTime.UtcNow;
        var open = await _context.UserTenantRoles
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId && r.TenantId == tenantId && r.RevokedAt == null)
            .ToListAsync();

        foreach (var row in open)
            row.RevokedAt = now;

        await _context.SaveChangesAsync();
    }
}
