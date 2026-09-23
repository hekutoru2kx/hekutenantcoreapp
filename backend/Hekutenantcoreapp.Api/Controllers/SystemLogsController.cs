using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

// SuperAdmin-only log viewer, backed by SystemLogsRepository's raw query against system_logs —
// deliberately the one place in this app where a caller can see every tenant's data at once
// (gated by LoggingSettingsPermission.Read, not tenant membership), since that's the point.
[ApiController]
[Route("api/system/platform/logs")]
[Authorize]
public class SystemLogsController : ControllerBase
{
    private readonly ISystemLogsService _service;
    private readonly ICategoryLogger _categoryLogger;

    public SystemLogsController(ISystemLogsService service, ICategoryLogger categoryLogger)
    {
        _service = service;
        _categoryLogger = categoryLogger;
    }

    [HttpGet]
    [Authorize(Policy = nameof(LoggingSettingsPermission) + "." + nameof(LoggingSettingsPermission.Read))]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int? tenantId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? level = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var result = await _service.QueryAsync(new SystemLogsQuery
            {
                TenantId = tenantId,
                Category = category,
                Level = level,
                From = from,
                To = to,
                Page = page,
                PageSize = pageSize
            });

            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            _categoryLogger.LogError(LogCategory.Http, $"Unhandled exception in {nameof(SystemLogsController)}", ex);
            return BadRequest(ex.Message);
        }
    }

    private static SystemLogsPageDto MapToDto(SystemLogsPageResult result) => new()
    {
        TotalCount = result.TotalCount,
        Items = result.Items.Select(i => new SystemLogEntryDto
        {
            Id = i.Id,
            Timestamp = i.Timestamp,
            Level = i.Level,
            Category = i.Category,
            Message = i.Message,
            Exception = i.Exception,
            TenantId = i.TenantId,
            UserId = i.UserId,
            TraceId = i.TraceId
        }).ToList()
    };
}
