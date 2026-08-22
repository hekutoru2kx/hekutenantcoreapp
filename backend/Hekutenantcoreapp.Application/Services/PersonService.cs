using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Application.Services;

public class PersonService : IPersonService
{
    private readonly IPersonRepository _repository;
    private readonly ITenantMembershipRepository _tenantMembershipRepository;
    private readonly IStringLocalizer<Messages> _localizer;

    public PersonService(
        IPersonRepository repository,
        ITenantMembershipRepository tenantMembershipRepository,
        IStringLocalizer<Messages> localizer)
    {
        _repository = repository;
        _tenantMembershipRepository = tenantMembershipRepository;
        _localizer = localizer;
    }

    public async Task<PagedResult<PersonResult>> GetPersonsAsync(PersonListQuery query) =>
        await _repository.GetPersonsAsync(query);

    public async Task<IList<PersonResult>> GetAllPersonsAsync(string? search, string? sortBy, string? sortDirection, int? countryId) =>
        await _repository.GetAllPersonsAsync(search, sortBy, sortDirection, countryId);

    public async Task<PersonResult?> GetPersonByIdAsync(int personId) =>
        await _repository.GetPersonByIdAsync(personId);

    public async Task<PersonResult> CreatePersonAsync(UpsertPersonRequest request) =>
        await _repository.CreatePersonAsync(request);

    public async Task UpdatePersonAsync(int personId, UpsertPersonRequest request) =>
        await _repository.UpdatePersonAsync(personId, request);

    public async Task LinkUserAsync(int personId, string userId) =>
        await _repository.LinkUserAsync(personId, userId);

    public async Task UnlinkUserAsync(int personId) =>
        await _repository.UnlinkUserAsync(personId);

    public async Task SuspendAccessAsync(int personId)
    {
        var person = await GetLinkedPersonAsync(personId);
        await _tenantMembershipRepository.SuspendAsync(person.UserId!, person.TenantId);
    }

    public async Task ReactivateAccessAsync(int personId)
    {
        var person = await GetLinkedPersonAsync(personId);
        await _tenantMembershipRepository.ActivateAsync(person.UserId!, person.TenantId);
    }

    private async Task<PersonResult> GetLinkedPersonAsync(int personId)
    {
        var person = await _repository.GetPersonByIdAsync(personId)
            ?? throw new Exception(_localizer["PersonNotFound"]);

        if (string.IsNullOrEmpty(person.UserId))
            throw new Exception(_localizer["PersonNotLinkedToUser"]);

        return person;
    }
}
