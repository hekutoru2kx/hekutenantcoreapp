using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/admin/organization/persons")]
[Authorize]
public class PersonController : ControllerBase
{
    private readonly IPersonService _personService;

    public PersonController(IPersonService personService)
    {
        _personService = personService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPersons(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? sortBy = "LastName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null,
        [FromQuery] int? countryId = null)
    {
        var result = await _personService.GetPersonsAsync(new PersonListQuery
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection,
            Search = search,
            CountryId = countryId
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
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Read))]
    public async Task<IActionResult> ExportPersons(
        [FromQuery] string? sortBy = "LastName",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string? search = null,
        [FromQuery] int? countryId = null)
    {
        try
        {
            var results = await _personService.GetAllPersonsAsync(search, sortBy, sortDirection, countryId);
            return Ok(results.Select(MapToDto));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPerson(int id)
    {
        var person = await _personService.GetPersonByIdAsync(id);
        if (person == null) return NotFound();
        return Ok(MapToDto(person));
    }

    [HttpPost]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Create))]
    public async Task<IActionResult> CreatePerson(UpsertPersonDto dto)
    {
        try
        {
            var result = await _personService.CreatePersonAsync(MapToRequest(dto));
            return Ok(MapToDto(result));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Update))]
    public async Task<IActionResult> UpdatePerson(int id, UpsertPersonDto dto)
    {
        try
        {
            await _personService.UpdatePersonAsync(id, MapToRequest(dto));
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/link-user/{userId}")]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Update))]
    public async Task<IActionResult> LinkUser(int id, string userId)
    {
        try
        {
            await _personService.LinkUserAsync(id, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/unlink-user")]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Update))]
    public async Task<IActionResult> UnlinkUser(int id)
    {
        try
        {
            await _personService.UnlinkUserAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/suspend-access")]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Update))]
    public async Task<IActionResult> SuspendAccess(int id)
    {
        try
        {
            await _personService.SuspendAccessAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}/reactivate-access")]
    [Authorize(Policy = nameof(PersonsPermission) + "." + nameof(PersonsPermission.Update))]
    public async Task<IActionResult> ReactivateAccess(int id)
    {
        try
        {
            await _personService.ReactivateAccessAsync(id);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static PersonDto MapToDto(PersonResult person) => new()
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
        CityId = person.CityId,
        CountryName = person.CountryName,
        StateName = person.StateName,
        CityName = person.CityName,
        LinkedUserName = person.LinkedUserName,
        MembershipStatus = person.MembershipStatus
    };

    private static UpsertPersonRequest MapToRequest(UpsertPersonDto dto) => new()
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
    };
}