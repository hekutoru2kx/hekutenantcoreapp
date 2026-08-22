using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/system/identity/users")]
[Authorize]
public class UserManagementController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Read))]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = "UserName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null,
        [FromQuery] string? roleFilter = null,
        [FromQuery] bool? statusFilter = null)
    {
        var query = new UserListQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Search = search,
            RoleFilter = roleFilter,
            StatusFilter = statusFilter
        };

        var result = await _userManagementService.GetUsersAsync(query);

        return Ok(new
        {
            items = result.Items.Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Roles = u.Roles,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                CreatedAt = u.CreatedAt,
                DefaultTenantId = u.DefaultTenantId
            }),
            totalCount = result.TotalCount,
            page = result.Page,
            pageSize = result.PageSize
        });
    }

    [HttpGet("export")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Read))]
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? sortBy = "UserName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null,
        [FromQuery] string? roleFilter = null,
        [FromQuery] bool? statusFilter = null)
    {
        try
        {
            var results = await _userManagementService.GetAllUsersAsync(search, sortBy, sortDirection, roleFilter, statusFilter);
            return Ok(results.Select(u => new UserListDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Roles = u.Roles,
                IsActive = u.IsActive,
                MustChangePassword = u.MustChangePassword,
                CreatedAt = u.CreatedAt,
                DefaultTenantId = u.DefaultTenantId
            }));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Create))]
    public async Task<IActionResult> CreateUser(CreateUserDto dto)
    {
        try
        {
            var password = string.IsNullOrEmpty(dto.Password)
                ? GeneratePassword()
                : dto.Password;

            var result = await _userManagementService.CreateUserAsync(new CreateUserRequest
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Password = password,
                Role = dto.Role
            });

            return Ok(new CreateUserResponseDto
            {
                UserId = result.UserId,
                Email = result.Email,
                UserName = result.UserName,
                TemporaryPassword = result.TemporaryPassword
            });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/grant-superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> GrantSuperAdmin(string id)
    {
        try
        {
            await _userManagementService.GrantSuperAdminAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/revoke-superadmin")]
    [Authorize(Roles = "SuperAdmin")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> RevokeSuperAdmin(string id)
    {
        try
        {
            await _userManagementService.RevokeSuperAdminAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/deactivate")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> Deactivate(string id)
    {
        try
        {
            await _userManagementService.DeactivateUserAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/activate")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> Activate(string id)
    {
        try
        {
            await _userManagementService.ActivateUserAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/reset-password")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> ResetPassword(string id, ResetPasswordDto dto)
    {
        try
        {
            var password = string.IsNullOrEmpty(dto.NewPassword)
                ? GeneratePassword()
                : dto.NewPassword;

            await _userManagementService.ResetPasswordAsync(id, password);
            return Ok(new { temporaryPassword = password });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}/tenants")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Read))]
    public async Task<IActionResult> GetUserTenants(string id)
    {
        var tenants = await _userManagementService.GetUserTenantsAsync(id);
        return Ok(tenants.Select(t => new TenantSummaryDto { Id = t.Id, Name = t.Name }));
    }

    [HttpPut("{id}/default-tenant")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update))]
    public async Task<IActionResult> SetDefaultTenant(string id, SetDefaultTenantDto dto)
    {
        try
        {
            await _userManagementService.SetDefaultTenantAsync(id, dto.TenantId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Delete))]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            await _userManagementService.DeleteUserAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Range(0, 12)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
        return password + "A1!";
    }
}