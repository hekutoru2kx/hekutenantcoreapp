using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class TenantRepository : ITenantRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly ExportSettings _exportSettings;

    public TenantRepository(HekutenantcoreappDbContext context, IStringLocalizer<Messages> localizer, ExportSettings exportSettings)
    {
        _context = context;
        _localizer = localizer;
        _exportSettings = exportSettings;
    }

    public async Task<PagedResult<TenantResult>> GetTenantsAsync(TenantListQuery query)
    {
        var tenantsQuery = ApplyFilterAndSort(_context.Tenants.AsQueryable(), query.Search, query.SortBy, query.SortDirection);

        var totalCount = await tenantsQuery.CountAsync();

        var tenants = await tenantsQuery
            .Include(t => t.Country)
            .Include(t => t.State)
            .Include(t => t.City)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<TenantResult>
        {
            Items = tenants.Select(MapToResult).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IList<TenantResult>> GetAllTenantsAsync(string? search, string? sortBy, string? sortDirection)
    {
        var tenantsQuery = ApplyFilterAndSort(_context.Tenants.AsQueryable(), search, sortBy, sortDirection);

        var totalCount = await tenantsQuery.CountAsync();
        if (!_exportSettings.IsUnlimited && totalCount > _exportSettings.MaxRows)
            throw new Exception(_localizer["ExportTooLarge", _exportSettings.MaxRows!.Value]);

        var tenants = await tenantsQuery
            .Include(t => t.Country)
            .Include(t => t.State)
            .Include(t => t.City)
            .ToListAsync();

        return tenants.Select(MapToResult).ToList();
    }

    private static IQueryable<Tenant> ApplyFilterAndSort(IQueryable<Tenant> tenantsQuery, string? search, string? sortBy, string? sortDirection)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            tenantsQuery = tenantsQuery.Where(t => t.Name.ToLower().Contains(term));
        }

        return sortBy?.ToLower() switch
        {
            "createdat" => sortDirection == "desc"
                ? tenantsQuery.OrderByDescending(t => t.CreatedAt)
                : tenantsQuery.OrderBy(t => t.CreatedAt),
            _ => sortDirection == "desc"
                ? tenantsQuery.OrderByDescending(t => t.Name)
                : tenantsQuery.OrderBy(t => t.Name)
        };
    }

    public async Task<TenantResult?> GetTenantByIdAsync(int tenantId)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Country)
            .Include(t => t.State)
            .Include(t => t.City)
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return tenant == null ? null : MapToResult(tenant);
    }

    public async Task<TenantResult> CreateTenantAsync(UpsertTenantRequest request)
    {
        var tenant = MapFromRequest(request);
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();

        return MapToResult(tenant);
    }

    public async Task UpdateTenantAsync(int tenantId, UpsertTenantRequest request)
    {
        var tenant = await _context.Tenants.FindAsync(tenantId)
            ?? throw new Exception(_localizer["TenantNotFound"]);

        UpdateFromRequest(tenant, request);
        await _context.SaveChangesAsync();
    }

    public async Task<IList<TenantSummaryResult>> GetActiveTenantSummariesAsync(string? search = null)
    {
        var query = _context.Tenants.Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(t => t.Name.ToLower().Contains(term));
        }

        query = query.OrderBy(t => t.Name);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Take(20);

        return await query
            .Select(t => new TenantSummaryResult { Id = t.Id, Name = t.Name })
            .ToListAsync();
    }

    public async Task<bool> IsActiveTenantAsync(int tenantId)
    {
        return await _context.Tenants.AnyAsync(t => t.Id == tenantId && t.IsActive);
    }

    private static TenantResult MapToResult(Tenant tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        TenantType = tenant.TenantType.ToString(),
        CountryId = tenant.CountryId,
        StateId = tenant.StateId,
        CityId = tenant.CityId,
        CountryName = tenant.Country?.Name,
        StateName = tenant.State?.Name,
        CityName = tenant.City?.Name,
        Phone = tenant.Phone,
        UrlSite = tenant.UrlSite,
        Email = tenant.Email,
        IsActive = tenant.IsActive,
        AttachmentRetentionDays = tenant.AttachmentRetentionDays
    };

    private static Tenant MapFromRequest(UpsertTenantRequest request)
    {
        var tenant = new Tenant();
        UpdateFromRequest(tenant, request);
        return tenant;
    }

    private static void UpdateFromRequest(Tenant tenant, UpsertTenantRequest request)
    {
        tenant.Name = request.Name;
        tenant.TenantType = request.TenantType != null ? Enum.Parse<TenantType>(request.TenantType) : TenantType.Standard;
        tenant.CountryId = request.CountryId;
        tenant.StateId = request.StateId;
        tenant.CityId = request.CityId;
        tenant.Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone;
        tenant.UrlSite = string.IsNullOrEmpty(request.UrlSite) ? null : request.UrlSite;
        tenant.Email = string.IsNullOrEmpty(request.Email) ? null : request.Email;
        tenant.IsActive = request.IsActive;
        tenant.AttachmentRetentionDays = request.AttachmentRetentionDays;
    }
}
