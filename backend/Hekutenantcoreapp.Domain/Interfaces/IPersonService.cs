using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IPersonService
{
    Task<PagedResult<PersonResult>> GetPersonsAsync(PersonListQuery query);
    Task<IList<PersonResult>> GetAllPersonsAsync(string? search, string? sortBy, string? sortDirection, int? countryId);
    Task<PersonResult?> GetPersonByIdAsync(int personId);
    Task<PersonResult> CreatePersonAsync(UpsertPersonRequest request);
    Task UpdatePersonAsync(int personId, UpsertPersonRequest request);
    Task LinkUserAsync(int personId, string userId);
    Task UnlinkUserAsync(int personId);

    // Tenant-scoped access control for the linked account — distinct from the account's
    // own global ApplicationUser.IsActive (System-area, SuperAdmin-only).
    Task SuspendAccessAsync(int personId);
    Task ReactivateAccessAsync(int personId);
}