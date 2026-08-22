using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class TenantService : ITenantService
{
    private readonly ITenantRepository _repository;

    public TenantService(ITenantRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<TenantResult>> GetTenantsAsync(TenantListQuery query) =>
        await _repository.GetTenantsAsync(query);

    public async Task<IList<TenantResult>> GetAllTenantsAsync(string? search, string? sortBy, string? sortDirection) =>
        await _repository.GetAllTenantsAsync(search, sortBy, sortDirection);

    public async Task<TenantResult?> GetTenantByIdAsync(int tenantId) =>
        await _repository.GetTenantByIdAsync(tenantId);

    public async Task<TenantResult> CreateTenantAsync(UpsertTenantRequest request) =>
        await _repository.CreateTenantAsync(request);

    public async Task UpdateTenantAsync(int tenantId, UpsertTenantRequest request) =>
        await _repository.UpdateTenantAsync(tenantId, request);

    public async Task<IList<TenantSummaryResult>> GetActiveTenantSummariesAsync(string? search = null) =>
        await _repository.GetActiveTenantSummariesAsync(search);
}
