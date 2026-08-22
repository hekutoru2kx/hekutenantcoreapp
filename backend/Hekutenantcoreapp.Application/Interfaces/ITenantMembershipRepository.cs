using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface ITenantMembershipRepository
{
    // Cross-tenant lookup (bypasses the ITenantScoped query filter) — used to resolve
    // which tenants a user can pick from, independent of whatever tenant (if any) their
    // current token claims.
    Task<IList<TenantSummaryResult>> GetMembershipTenantsForUserAsync(string userId);
    Task<bool> HasActiveMembershipAsync(string userId, int tenantId);
    Task<bool> HasAnyMembershipAsync(string userId, int tenantId);
    Task<Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus?> GetStatusAsync(string userId, int tenantId);
    Task CreateAsync(string userId, int tenantId, Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus status);
    Task ActivateAsync(string userId, int tenantId);

    // Blocks ALL access to the tenant for this user (enforced live in ActiveUserMiddleware) —
    // the deliberate "no relationship with this tenant at all" action, distinct from
    // Employee.IsActive which only affects staff-side capability.
    Task SuspendAsync(string userId, int tenantId);
}
