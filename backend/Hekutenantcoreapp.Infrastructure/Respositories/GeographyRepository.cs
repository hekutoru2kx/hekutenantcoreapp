using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class GeographyRepository : IGeographyRepository
{
    private readonly HekutenantcoreappDbContext _context;

    public GeographyRepository(HekutenantcoreappDbContext context)
    {
        _context = context;
    }

    public async Task<IList<Country>> GetCountriesAsync() =>
        await _context.Countries
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<IList<State>> GetStatesByCountryAsync(int countryId) =>
        await _context.States
            .Where(s => s.CountryId == countryId)
            .OrderBy(s => s.Name)
            .ToListAsync();

    public async Task<IList<City>> GetCitiesByStateAsync(int stateId) =>
        await _context.Cities
            .Where(c => c.StateId == stateId)
            .OrderBy(c => c.Name)
            .ToListAsync();
}