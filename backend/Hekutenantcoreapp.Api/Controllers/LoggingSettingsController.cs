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
[Route("api/system/platform/logging-settings")]
[Authorize]
public class LoggingSettingsController : ControllerBase
{
    private readonly ILoggingSettingsService _service;
    private readonly ICategoryLogger _categoryLogger;

    public LoggingSettingsController(ILoggingSettingsService service, ICategoryLogger categoryLogger)
    {
        _service = service;
        _categoryLogger = categoryLogger;
    }

    [HttpGet]
    [Authorize(Policy = nameof(LoggingSettingsPermission) + "." + nameof(LoggingSettingsPermission.Read))]
    public async Task<IActionResult> GetSettings()
    {
        var result = await _service.GetSettingsAsync();
        return Ok(MapToDto(result));
    }

    [HttpPut]
    [Authorize(Policy = nameof(LoggingSettingsPermission) + "." + nameof(LoggingSettingsPermission.Update))]
    public async Task<IActionResult> UpdateSettings(UpdateLoggingSettingsDto dto)
    {
        try
        {
            await _service.UpdateSettingsAsync(MapToRequest(dto));
            return Ok();
        }
        catch (Exception ex)
        {
            _categoryLogger.LogError(LogCategory.Http, $"Unhandled exception in {nameof(LoggingSettingsController)}", ex);
            return BadRequest(ex.Message);
        }
    }

    private static LoggingSettingsDto MapToDto(LoggingSettingsResult result) => new()
    {
        Categories = result.Categories.Select(c => new LoggingCategorySettingDto { Category = c.Category, MinimumLevel = c.MinimumLevel }).ToList(),
        RetentionDays = result.RetentionDays
    };

    private static UpdateLoggingSettingsRequest MapToRequest(UpdateLoggingSettingsDto dto) => new()
    {
        Categories = dto.Categories.Select(c => new UpdateLoggingCategorySettingRequest { Category = c.Category, MinimumLevel = c.MinimumLevel }).ToList(),
        RetentionDays = dto.RetentionDays
    };
}
