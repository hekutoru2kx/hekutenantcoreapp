using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IPersonRepository
{
    Task<PagedResult<PersonResult>> GetPersonsAsync(PersonListQuery query);
    Task<IList<PersonResult>> GetAllPersonsAsync(string? search, string? sortBy, string? sortDirection, int? countryId);
    Task<PersonResult?> GetPersonByIdAsync(int personId);
    Task<PersonResult> CreatePersonAsync(UpsertPersonRequest request);
    Task UpdatePersonAsync(int personId, UpsertPersonRequest request);
    Task LinkUserAsync(int personId, string userId);
    Task UnlinkUserAsync(int personId);
}