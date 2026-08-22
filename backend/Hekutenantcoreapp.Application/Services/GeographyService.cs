using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Interfaces;

namespace Hekutenantcoreapp.Application.Services;

public class GeographyService : IGeographyService
{
    private readonly IGeographyRepository _repository;

    public GeographyService(IGeographyRepository repository)
    {
        _repository = repository;
    }

    public async Task<IList<Country>> GetCountriesAsync() =>
        await _repository.GetCountriesAsync();

    public async Task<IList<State>> GetStatesByCountryAsync(int countryId) =>
        await _repository.GetStatesByCountryAsync(countryId);

    public async Task<IList<City>> GetCitiesByStateAsync(int stateId) =>
        await _repository.GetCitiesByStateAsync(stateId);
}