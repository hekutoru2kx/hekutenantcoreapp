using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface ITenantService
{
    Task<PagedResult<TenantResult>> GetTenantsAsync(TenantListQuery query);
    Task<IList<TenantResult>> GetAllTenantsAsync(string? search, string? sortBy, string? sortDirection);
    Task<TenantResult?> GetTenantByIdAsync(int tenantId);
    Task<TenantResult> CreateTenantAsync(UpsertTenantRequest request);
    Task UpdateTenantAsync(int tenantId, UpsertTenantRequest request);
    Task<IList<TenantSummaryResult>> GetActiveTenantSummariesAsync(string? search = null);
}
