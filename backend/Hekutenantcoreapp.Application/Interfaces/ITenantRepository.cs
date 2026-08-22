using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface ITenantRepository
{
    Task<PagedResult<TenantResult>> GetTenantsAsync(TenantListQuery query);
    Task<IList<TenantResult>> GetAllTenantsAsync(string? search, string? sortBy, string? sortDirection);
    Task<TenantResult?> GetTenantByIdAsync(int tenantId);
    Task<TenantResult> CreateTenantAsync(UpsertTenantRequest request);
    Task UpdateTenantAsync(int tenantId, UpsertTenantRequest request);
    // No search term: full active list (register picker, SuperAdmin's own tenant list).
    // With a search term: name-contains match, capped — so a "join another tenant" flow can
    // offer search-to-reveal instead of always rendering every tenant in the system.
    Task<IList<TenantSummaryResult>> GetActiveTenantSummariesAsync(string? search = null);
    Task<bool> IsActiveTenantAsync(int tenantId);
}
