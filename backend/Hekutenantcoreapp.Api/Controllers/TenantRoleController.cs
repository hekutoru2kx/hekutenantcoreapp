using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

// Read-only view of the shared role catalog for tenant admins: lets them review a role's
// claims before assigning it to an employee. Creating roles or editing claims is
// SuperAdmin-only, via RoleManagementController (api/system/identity/roles).
[ApiController]
[Route("api/admin/identity/roles")]
[Authorize]
public class TenantRoleController : ControllerBase
{
    private readonly ITenantRoleService _tenantRoleService;

    public TenantRoleController(ITenantRoleService tenantRoleService)
    {
        _tenantRoleService = tenantRoleService;
    }

    [HttpGet]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _tenantRoleService.GetVisibleRolesAsync();
        return Ok(roles.Select(r => new TenantRoleDto
        {
            Name = r.Name,
            Claims = r.Claims.Select(c => new PermissionClaimDto { Module = c.Module, Action = c.Action }).ToList()
        }));
    }
}
