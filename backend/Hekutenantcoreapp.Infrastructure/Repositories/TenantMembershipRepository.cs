using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class TenantMembershipRepository : ITenantMembershipRepository
{
    private readonly HekutenantcoreappDbContext _context;

    public TenantMembershipRepository(HekutenantcoreappDbContext context)
    {
        _context = context;
    }

    public async Task<IList<TenantSummaryResult>> GetMembershipTenantsForUserAsync(string userId)
    {
        return await _context.TenantMemberships
            .IgnoreQueryFilters()
            .Where(m => m.UserId == userId && m.Status != TenantMembershipStatus.Suspended)
            .Include(m => m.Tenant)
            .Where(m => m.Tenant != null && m.Tenant.IsActive)
            .Select(m => new TenantSummaryResult { Id = m.TenantId, Name = m.Tenant!.Name })
            .ToListAsync();
    }

    public async Task<bool> HasActiveMembershipAsync(string userId, int tenantId)
    {
        return await _context.TenantMemberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.Status != TenantMembershipStatus.Suspended);
    }

    public async Task<bool> HasAnyMembershipAsync(string userId, int tenantId)
    {
        return await _context.TenantMemberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId);
    }

    public async Task<TenantMembershipStatus?> GetStatusAsync(string userId, int tenantId)
    {
        var membership = await _context.TenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);

        return membership?.Status;
    }

    public async Task CreateAsync(string userId, int tenantId, TenantMembershipStatus status)
    {
        _context.TenantMemberships.Add(new TenantMembership
        {
            UserId = userId,
            TenantId = tenantId,
            Status = status
        });
        await _context.SaveChangesAsync();
    }

    public async Task ActivateAsync(string userId, int tenantId)
    {
        var membership = await _context.TenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);

        if (membership == null || membership.Status == TenantMembershipStatus.Active) return;

        membership.Status = TenantMembershipStatus.Active;
        await _context.SaveChangesAsync();
    }

    public async Task SuspendAsync(string userId, int tenantId)
    {
        var membership = await _context.TenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.UserId == userId && m.TenantId == tenantId);

        if (membership == null || membership.Status == TenantMembershipStatus.Suspended) return;

        membership.Status = TenantMembershipStatus.Suspended;
        await _context.SaveChangesAsync();
    }
}
