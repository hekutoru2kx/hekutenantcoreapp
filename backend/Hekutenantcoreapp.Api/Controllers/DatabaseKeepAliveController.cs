using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/system/platform/database-keep-alive")]
[Authorize]
public class DatabaseKeepAliveController : ControllerBase
{
    private readonly IDatabaseKeepAliveService _service;

    public DatabaseKeepAliveController(IDatabaseKeepAliveService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = nameof(DatabaseKeepAlivePermission) + "." + nameof(DatabaseKeepAlivePermission.Read))]
    public async Task<IActionResult> GetSettings()
    {
        var result = await _service.GetSettingsAsync();
        return Ok(MapToDto(result));
    }

    [HttpPut]
    [Authorize(Policy = nameof(DatabaseKeepAlivePermission) + "." + nameof(DatabaseKeepAlivePermission.Update))]
    public async Task<IActionResult> UpdateSettings(UpdateDatabaseKeepAliveSettingsDto dto)
    {
        try
        {
            await _service.UpdateSettingsAsync(MapToRequest(dto));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static DatabaseKeepAliveSettingsDto MapToDto(DatabaseKeepAliveSettingsResult result) => new()
    {
        IsEnabled = result.IsEnabled,
        IntervalAmount = result.IntervalAmount,
        IntervalUnit = result.IntervalUnit,
        GmtOffsetHours = result.GmtOffsetHours,
        ActiveStartTime = result.ActiveStartTime,
        ActiveEndTime = result.ActiveEndTime,
        LastPingAt = result.LastPingAt
    };

    private static UpdateDatabaseKeepAliveSettingsRequest MapToRequest(UpdateDatabaseKeepAliveSettingsDto dto) => new()
    {
        IsEnabled = dto.IsEnabled,
        IntervalAmount = dto.IntervalAmount,
        IntervalUnit = dto.IntervalUnit,
        GmtOffsetHours = dto.GmtOffsetHours,
        ActiveStartTime = dto.ActiveStartTime,
        ActiveEndTime = dto.ActiveEndTime
    };
}
