using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Hekutenantcoreapp.Domain.Models;
using System.Security.Claims;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IMultiTenantSettingsService _multiTenantSettingsService;

    public AuthController(IAuthService authService, IMultiTenantSettingsService multiTenantSettingsService)
    {
        _authService = authService;
        _multiTenantSettingsService = multiTenantSettingsService;
    }

    // [AllowAnonymous] pre-auth lookup for the Register page: whether it should show a tenant
    // dropdown at all, and what to fall back to when it doesn't.
    [HttpGet("registration-tenant-info")]
    [AllowAnonymous]
    public async Task<IActionResult> RegistrationTenantInfo()
    {
        var settings = await _multiTenantSettingsService.GetSettingsAsync();
        return Ok(new RegistrationTenantInfoDto
        {
            TenantSelectionEnabled = !(settings.DefaultTenantLoginEnabled || settings.MultiTenantDisabled),
            DefaultTenantId = settings.DefaultTenantId,
            DefaultTenantName = settings.DefaultTenantName
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        try
        {
            var result = await _authService.RegisterAsync(new RegisterRequest
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Password = dto.Password,
                TenantId = dto.TenantId
            });
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto.Email, dto.Password);
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("google")]
    public async Task<IActionResult> GoogleAuth(GoogleAuthDto dto)
    {
        try
        {
            var result = await _authService.LoginOrRegisterWithGoogleAsync(dto.IdToken, dto.TenantId);
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("select-tenant")]
    [Authorize]
    public async Task<IActionResult> SelectTenant(SelectTenantDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _authService.SelectTenantAsync(userId, dto.TenantId);
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("join-tenant")]
    [Authorize]
    public async Task<IActionResult> JoinTenant(SelectTenantDto dto)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _authService.JoinTenantAsPatientAsync(userId, dto.TenantId);
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("available-tenants")]
    [Authorize]
    public async Task<IActionResult> AvailableTenants()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var tenants = await _authService.GetAvailableTenantsAsync(userId);
        return Ok(tenants.Select(t => new TenantSummaryDto { Id = t.Id, Name = t.Name }));
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
    {
        try
        {
            await _authService.AssignRoleAsync(dto.Email, dto.Role);
            return Ok(new { message = $"{dto.Email} assigned to {dto.Role}" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static AuthResponseDto MapToDto(AuthResult result) => new()
    {
        Token = result.Token,
        Email = result.Email,
        UserName = result.UserName,
        MustChangePassword = result.MustChangePassword,
        PreferredTheme = result.PreferredTheme,
        TenantId = result.TenantId,
        TenantName = result.TenantName,
        AvailableTenants = result.AvailableTenants.Select(t => new TenantSummaryDto { Id = t.Id, Name = t.Name }).ToList(),
        MultiTenantDisabled = result.MultiTenantDisabled
    };
}
