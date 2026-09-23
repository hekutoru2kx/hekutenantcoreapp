using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/system/platform/multi-tenant-settings")]
[Authorize]
public class MultiTenantSettingsController : ControllerBase
{
    private readonly IMultiTenantSettingsService _service;
    private readonly ICategoryLogger _categoryLogger;

    public MultiTenantSettingsController(IMultiTenantSettingsService service, ICategoryLogger categoryLogger)
    {
        _service = service;
        _categoryLogger = categoryLogger;
    }

    [HttpGet]
    [Authorize(Policy = nameof(MultiTenantSettingsPermission) + "." + nameof(MultiTenantSettingsPermission.Read))]
    public async Task<IActionResult> GetSettings()
    {
        var result = await _service.GetSettingsAsync();
        return Ok(MapToDto(result));
    }

    [HttpPut]
    [Authorize(Policy = nameof(MultiTenantSettingsPermission) + "." + nameof(MultiTenantSettingsPermission.Update))]
    public async Task<IActionResult> UpdateSettings(UpdateMultiTenantSettingsDto dto)
    {
        try
        {
            await _service.UpdateSettingsAsync(MapToRequest(dto));
            return Ok();
        }
        catch (Exception ex)
        {
            _categoryLogger.LogError(LogCategory.Http, $"Unhandled exception in {nameof(MultiTenantSettingsController)}", ex);
            return BadRequest(ex.Message);
        }
    }

    private static MultiTenantSettingsDto MapToDto(MultiTenantSettingsResult result) => new()
    {
        DefaultTenantLoginEnabled = result.DefaultTenantLoginEnabled,
        MultiTenantDisabled = result.MultiTenantDisabled,
        DefaultTenantId = result.DefaultTenantId,
        DefaultTenantName = result.DefaultTenantName
    };

    private static UpdateMultiTenantSettingsRequest MapToRequest(UpdateMultiTenantSettingsDto dto) => new()
    {
        DefaultTenantLoginEnabled = dto.DefaultTenantLoginEnabled,
        MultiTenantDisabled = dto.MultiTenantDisabled,
        DefaultTenantId = dto.DefaultTenantId
    };
}
