using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authorizationService;

    public UserController(IUserService userService, IAuthorizationService authorizationService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
    }

    [HttpPut("language")]
    [Authorize]
    public async Task<IActionResult> UpdateLanguage(UpdateLanguageDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        await _userService.UpdateLanguageAsync(userId, dto.Language);
        return Ok();
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await _userService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var profile = await _userService.GetProfileAsync(userId);
        return Ok(new UserProfileDto
        {
            Id = profile.Id,
            UserName = profile.UserName,
            Email = profile.Email,
            PreferredLanguage = profile.PreferredLanguage,
            PreferredTheme = profile.PreferredTheme,
            Roles = profile.Roles,
            DefaultTenantId = profile.DefaultTenantId
        });
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(UpdateProfileDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        try
        {
            await _userService.UpdateProfileAsync(new UpdateProfileRequest
            {
                UserId = userId,
                UserName = dto.UserName,
                Email = dto.Email,
                PreferredLanguage = dto.PreferredLanguage,
                PreferredTheme = dto.PreferredTheme,
                DefaultTenantId = dto.DefaultTenantId
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("person")]
    [Authorize]
    public async Task<IActionResult> GetPerson()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var person = await _userService.GetPersonAsync(userId);
        if (person == null) return Ok(null);

        return Ok(new PersonDto
        {
            Id = person.Id,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Birthday = person.Birthday,
            DocumentType = person.DocumentType,
            DocumentId = person.DocumentId,
            Phone = person.Phone,
            PhoneExtension = person.PhoneExtension,
            Email = person.Email,
            Address = person.Address,
            PostalCode = person.PostalCode,
            Gender = person.Gender,
            AlternativePhone = person.AlternativePhone,
            CountryId = person.CountryId,
            StateId = person.StateId,
            CityId = person.CityId
        });
    }

    [HttpGet("person/check-existing")]
    [Authorize]
    public async Task<IActionResult> CheckExistingPerson(
        [FromQuery] string? documentType, [FromQuery] string? documentId, [FromQuery] string? email)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (callerId == null) return Unauthorized();

        var result = await _userService.CheckExistingPersonAsync(callerId, documentType, documentId, email);
        return Ok(new { matchFound = result.MatchFound, linkable = result.Linkable });
    }

    [HttpPut("person")]
    [HttpPut("{userId}/person")]
    [Authorize]
    public async Task<IActionResult> UpsertPerson(string? userId, UpsertPersonDto dto)
    {
        var callerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (callerId == null) return Unauthorized();

        var targetUserId = userId ?? callerId;

        if (targetUserId != callerId)
        {
            var authResult = await _authorizationService.AuthorizeAsync(User,
                nameof(UserManagementPermission) + "." + nameof(UserManagementPermission.Update));
            if (!authResult.Succeeded) return Forbid();
        }

        try
        {
            await _userService.UpsertPersonAsync(targetUserId, new UpsertPersonRequest
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Birthday = dto.Birthday,
                DocumentType = dto.DocumentType,
                DocumentId = dto.DocumentId,
                Phone = dto.Phone,
                PhoneExtension = dto.PhoneExtension,
                Email = dto.Email,
                Address = dto.Address,
                PostalCode = dto.PostalCode,
                Gender = dto.Gender,
                AlternativePhone = dto.AlternativePhone,
                CountryId = dto.CountryId,
                StateId = dto.StateId,
                CityId = dto.CityId
            });
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}