using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/admin/organization/employees")]
[Authorize]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeeController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet("assignable-roles")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> GetAssignableRoles()
    {
        return Ok(await _employeeService.GetAssignableRoleNamesAsync());
    }

    [HttpGet]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = "JobTitle",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        var result = await _employeeService.GetEmployeesAsync(new EmployeeListQuery
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

    [HttpGet("export")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> ExportEmployees(
        [FromQuery] string? sortBy = "JobTitle",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null)
    {
        try
        {
            var results = await _employeeService.GetAllEmployeesAsync(search, sortBy, sortDirection);
            return Ok(results.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> GetEmployee(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null) return NotFound();
        return Ok(MapToDto(employee));
    }

    [HttpPost("invite")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Create))]
    public async Task<IActionResult> InviteEmployee(InviteEmployeeDto dto)
    {
        try
        {
            var result = await _employeeService.InviteEmployeeAsync(new InviteEmployeeRequest
            {
                Email = dto.Email,
                UserName = dto.UserName,
                RoleName = dto.RoleName,
                RoleExpiresAt = dto.RoleExpiresAt,
                JobTitle = dto.JobTitle,
                HireDate = dto.HireDate
            });

            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Update))]
    public async Task<IActionResult> UpdateEmployee(int id, UpsertEmployeeDto dto)
    {
        try
        {
            await _employeeService.UpdateEmployeeAsync(id, new UpsertEmployeeRequest
            {
                JobTitle = dto.JobTitle,
                HireDate = dto.HireDate,
                IsActive = dto.IsActive
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // Absolute route override — role assignment belongs to the Identity bounded context, not
    // Organization (which owns the rest of this controller), even though it's still keyed off
    // an Employee id. See hekutenantcoreapp-bounded-context-routing.
    [HttpGet("/api/admin/identity/employees/{id}/roles")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Read))]
    public async Task<IActionResult> GetRoleAssignments(int id)
    {
        try
        {
            var assignments = await _employeeService.GetRoleAssignmentsAsync(id);
            return Ok(assignments.Select(a => new RoleAssignmentDto
            {
                RoleName = a.RoleName,
                StartsAt = a.StartsAt,
                ExpiresAt = a.ExpiresAt,
                IsPending = a.IsPending,
                IsExpired = a.IsExpired,
                CreatedAt = a.CreatedAt,
                CreatedByName = a.CreatedByName,
                UpdatedAt = a.UpdatedAt,
                UpdatedByName = a.UpdatedByName
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("/api/admin/identity/employees/{id}/roles")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Update))]
    public async Task<IActionResult> AssignRoles(int id, AssignEmployeeRolesDto dto)
    {
        try
        {
            var assignments = dto.Roles.Select(r => new RoleAssignmentRequest
            {
                RoleName = r.RoleName,
                StartsAt = r.StartsAt,
                ExpiresAt = r.ExpiresAt
            }).ToList();

            await _employeeService.AssignRolesAsync(id, assignments);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/activate")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Update))]
    public async Task<IActionResult> Activate(int id)
    {
        try
        {
            await _employeeService.ActivateAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = nameof(EmployeesPermission) + "." + nameof(EmployeesPermission.Update))]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            await _employeeService.DeactivateAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static EmployeeDto MapToDto(EmployeeResult employee) => new()
    {
        Id = employee.Id,
        UserId = employee.UserId,
        UserName = employee.UserName,
        Email = employee.Email,
        JobTitle = employee.JobTitle,
        HireDate = employee.HireDate,
        IsActive = employee.IsActive,
        Roles = employee.Roles
    };
}
