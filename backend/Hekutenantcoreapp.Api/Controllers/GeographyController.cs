using Hekutenantcoreapp.Application.DTOs;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeographyController : ControllerBase
{
    private readonly IGeographyService _geographyService;

    public GeographyController(IGeographyService geographyService)
    {
        _geographyService = geographyService;
    }

    [HttpGet("countries")]
    public async Task<IActionResult> GetCountries()
    {
        var countries = await _geographyService.GetCountriesAsync();
        return Ok(countries.Select(c => new CountryDto
        {
            Id = c.Id,
            Name = c.Name,
            Iso2 = c.Iso2,
            PhoneCode = c.PhoneCode
        }));
    }

    [HttpGet("countries/{countryId}/states")]
    public async Task<IActionResult> GetStates(int countryId)
    {
        var states = await _geographyService.GetStatesByCountryAsync(countryId);
        return Ok(states.Select(s => new StateDto
        {
            Id = s.Id,
            Name = s.Name,
            StateCode = s.StateCode
        }));
    }

    [HttpGet("states/{stateId}/cities")]
    public async Task<IActionResult> GetCities(int stateId)
    {
        var cities = await _geographyService.GetCitiesByStateAsync(stateId);
        return Ok(cities.Select(c => new CityDto
        {
            Id = c.Id,
            Name = c.Name
        }));
    }
}