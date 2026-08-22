using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/system/platform/tenants")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public TenantController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    [Authorize(Policy = nameof(TenantsPermission) + "." + nameof(TenantsPermission.Read))]
    public async Task<IActionResult> GetTenants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = "Name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        var result = await _tenantService.GetTenantsAsync(new TenantListQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Search = search
        });

        return Ok(new
        {
            items = result.Items.Select(MapToDto),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize
        });
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActiveTenants([FromQuery] string? search = null)
    {
        var tenants = await _tenantService.GetActiveTenantSummariesAsync(search);
        return Ok(tenants.Select(t => new TenantSummaryDto { Id = t.Id, Name = t.Name }));
    }

    [HttpGet("export")]
    [Authorize(Policy = nameof(TenantsPermission) + "." + nameof(TenantsPermission.Read))]
    public async Task<IActionResult> ExportTenants(
        [FromQuery] string? sortBy = "Name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        try
        {
            var results = await _tenantService.GetAllTenantsAsync(search, sortBy, sortDirection);
            return Ok(results.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = nameof(TenantsPermission) + "." + nameof(TenantsPermission.Read))]
    public async Task<IActionResult> GetTenant(int id)
    {
        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null) return NotFound();
        return Ok(MapToDto(tenant));
    }

    [HttpPost]
    [Authorize(Policy = nameof(TenantsPermission) + "." + nameof(TenantsPermission.Create))]
    public async Task<IActionResult> CreateTenant(UpsertTenantDto dto)
    {
        try
        {
            var result = await _tenantService.CreateTenantAsync(MapToRequest(dto));
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(TenantsPermission) + "." + nameof(TenantsPermission.Update))]
    public async Task<IActionResult> UpdateTenant(int id, UpsertTenantDto dto)
    {
        try
        {
            await _tenantService.UpdateTenantAsync(id, MapToRequest(dto));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static TenantDto MapToDto(TenantResult tenant) => new()
    {
        Id = tenant.Id,
        Name = tenant.Name,
        TenantType = tenant.TenantType,
        CountryId = tenant.CountryId,
        StateId = tenant.StateId,
        CityId = tenant.CityId,
        CountryName = tenant.CountryName,
        StateName = tenant.StateName,
        CityName = tenant.CityName,
        Phone = tenant.Phone,
        UrlSite = tenant.UrlSite,
        Email = tenant.Email,
        IsActive = tenant.IsActive,
        AttachmentRetentionDays = tenant.AttachmentRetentionDays
    };

    private static UpsertTenantRequest MapToRequest(UpsertTenantDto dto) => new()
    {
        Name = dto.Name,
        TenantType = dto.TenantType,
        CountryId = dto.CountryId,
        StateId = dto.StateId,
        CityId = dto.CityId,
        Phone = dto.Phone,
        UrlSite = dto.UrlSite,
        Email = dto.Email,
        IsActive = dto.IsActive,
        AttachmentRetentionDays = dto.AttachmentRetentionDays
    };
}
