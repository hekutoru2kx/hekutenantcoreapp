using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Content;

// Resolves the caller's current tenant (HekutenantcoreappDbContext.CurrentTenantId — internal,
// same-assembly access, same value the ITenantScoped query filter uses) to that tenant's
// Tenant.StoragePrefix. Falls back to "global" when there's no resolvable tenant context
// (background/seed work, or a Tenant row somehow missing its prefix) rather than throwing —
// blob storage has no tenant-scoping invariant to protect the way a DB row does, so degrading
// to a shared bucket is an acceptable, recoverable choice a thrown exception wouldn't be.
public class TenantContentPartitionResolver : IContentPartitionResolver
{
    private readonly HekutenantcoreappDbContext _context;

    public TenantContentPartitionResolver(HekutenantcoreappDbContext context)
    {
        _context = context;
    }

    public async Task<string> ResolveAsync(CancellationToken ct = default)
    {
        var tenantId = _context.CurrentTenantId;
        if (tenantId == 0) return "global";

        // Tenant itself isn't ITenantScoped (it can't be scoped to itself), so no query filter
        // applies here regardless of CurrentTenantId.
        var prefix = await _context.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.StoragePrefix)
            .FirstOrDefaultAsync(ct);

        return string.IsNullOrEmpty(prefix) ? "global" : prefix;
    }
}
