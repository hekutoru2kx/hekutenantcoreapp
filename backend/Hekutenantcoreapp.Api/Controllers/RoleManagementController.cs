using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/system/identity/roles")]
[Authorize]
public class RoleManagementController : ControllerBase
{
    private readonly IRoleManagementService _roleManagementService;

    public RoleManagementController(IRoleManagementService roleManagementService)
    {
        _roleManagementService = roleManagementService;
    }

    [HttpGet]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Read))]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManagementService.GetRolesAsync();
        return Ok(roles.Select(MapToDto));
    }

    [HttpGet("permissions")]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Read))]
    public IActionResult GetPermissionCatalog()
    {
        var catalog = _roleManagementService.GetPermissionCatalog();
        return Ok(catalog.Select(m => new PermissionModuleDto
        {
            Module = m.Module,
            Actions = m.Actions
        }));
    }

    [HttpPost]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Create))]
    public async Task<IActionResult> CreateRole(CreateRoleDto dto)
    {
        try
        {
            await _roleManagementService.CreateRoleAsync(dto.Name);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{name}")]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Delete))]
    public async Task<IActionResult> DeleteRole(string name)
    {
        try
        {
            await _roleManagementService.DeleteRoleAsync(name);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("restore-defaults")]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Create))]
    public async Task<IActionResult> RestoreDefaultRoles()
    {
        await _roleManagementService.RestoreDefaultRolesAsync();
        return Ok();
    }

    [HttpPut("{name}/claims")]
    [Authorize(Policy = nameof(RolesPermission) + "." + nameof(RolesPermission.Update))]
    public async Task<IActionResult> AssignClaims(string name, AssignClaimsDto dto)
    {
        try
        {
            await _roleManagementService.AssignClaimsAsync(name, dto.Claims
                .Select(c => new PermissionClaimResult { Module = c.Module, Action = c.Action })
                .ToList());
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static RoleDto MapToDto(RoleResult role) => new()
    {
        Name = role.Name,
        Claims = role.Claims
            .Select(c => new PermissionClaimDto { Module = c.Module, Action = c.Action })
            .ToList()
    };
}
