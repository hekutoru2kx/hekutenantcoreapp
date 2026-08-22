using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class PersonRepository : IPersonRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly ExportSettings _exportSettings;

    public PersonRepository(HekutenantcoreappDbContext context, IStringLocalizer<Messages> localizer, ExportSettings exportSettings)
    {
        _context = context;
        _localizer = localizer;
        _exportSettings = exportSettings;
    }

    public async Task<PagedResult<PersonResult>> GetPersonsAsync(PersonListQuery query)
    {
        var personsQuery = ApplyFilterAndSort(_context.Persons.AsQueryable(), query.Search, query.SortBy, query.SortDirection, query.CountryId);

        var totalCount = await personsQuery.CountAsync();

        var persons = await personsQuery
            .Include(p => p.Country)
            .Include(p => p.State)
            .Include(p => p.City)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var (linkedUserNames, linkedStatuses) = await GetLinkedUserDataAsync(persons);

        return new PagedResult<PersonResult>
        {
            Items = persons.Select(p => MapToResult(
                p,
                p.UserId != null ? linkedUserNames.GetValueOrDefault(p.UserId) : null,
                p.UserId != null ? linkedStatuses.GetValueOrDefault(p.UserId) : null)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IList<PersonResult>> GetAllPersonsAsync(string? search, string? sortBy, string? sortDirection, int? countryId)
    {
        var personsQuery = ApplyFilterAndSort(_context.Persons.AsQueryable(), search, sortBy, sortDirection, countryId);

        var totalCount = await personsQuery.CountAsync();
        if (!_exportSettings.IsUnlimited && totalCount > _exportSettings.MaxRows)
            throw new Exception(_localizer["ExportTooLarge", _exportSettings.MaxRows!.Value]);

        var persons = await personsQuery
            .Include(p => p.Country)
            .Include(p => p.State)
            .Include(p => p.City)
            .ToListAsync();

        var (linkedUserNames, linkedStatuses) = await GetLinkedUserDataAsync(persons);

        return persons.Select(p => MapToResult(
            p,
            p.UserId != null ? linkedUserNames.GetValueOrDefault(p.UserId) : null,
            p.UserId != null ? linkedStatuses.GetValueOrDefault(p.UserId) : null)).ToList();
    }

    private static IQueryable<Person> ApplyFilterAndSort(IQueryable<Person> personsQuery, string? search, string? sortBy, string? sortDirection, int? countryId)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Split into words so a full-name search ("Jane Doe") matches a person whose first
            // name contains one word and last name contains the other, in either order — each
            // word must match somewhere (AND across words), but any field per word (OR across
            // FirstName/LastName/DocumentId), same as searching a single word always has.
            var words = search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in words)
            {
                personsQuery = personsQuery.Where(p =>
                    p.FirstName.ToLower().Contains(word) ||
                    p.LastName.ToLower().Contains(word) ||
                    (p.DocumentId != null && p.DocumentId.ToLower().Contains(word)));
            }
        }

        if (countryId.HasValue)
            personsQuery = personsQuery.Where(p => p.CountryId == countryId.Value);

        return sortBy?.ToLower() switch
        {
            "firstname" => sortDirection == "desc"
                ? personsQuery.OrderByDescending(p => p.FirstName)
                : personsQuery.OrderBy(p => p.FirstName),
            "email" => sortDirection == "desc"
                ? personsQuery.OrderByDescending(p => p.Email)
                : personsQuery.OrderBy(p => p.Email),
            "createdat" => sortDirection == "desc"
                ? personsQuery.OrderByDescending(p => p.CreatedAt)
                : personsQuery.OrderBy(p => p.CreatedAt),
            "countryid" => sortDirection == "desc"
                ? personsQuery.OrderByDescending(p => p.CountryId)
                : personsQuery.OrderBy(p => p.CountryId),
            _ => sortDirection == "desc"
                ? personsQuery.OrderByDescending(p => p.LastName)
                : personsQuery.OrderBy(p => p.LastName)
        };
    }

    private async Task<(Dictionary<string, string?> linkedUserNames, Dictionary<string, TenantMembershipStatus> linkedStatuses)> GetLinkedUserDataAsync(IList<Person> persons)
    {
        var linkedUserIds = persons.Where(p => p.UserId != null).Select(p => p.UserId!).ToList();
        var linkedUserNames = await _context.Users
            .Where(u => linkedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        // Already scoped to the same tenant as `persons` via the ambient ITenantScoped filter.
        var linkedStatuses = await _context.TenantMemberships
            .Where(m => linkedUserIds.Contains(m.UserId))
            .ToDictionaryAsync(m => m.UserId, m => m.Status);

        return (linkedUserNames, linkedStatuses);
    }

    public async Task<PersonResult?> GetPersonByIdAsync(int personId)
    {
        var person = await _context.Persons.FindAsync(personId);
        if (person == null) return null;

        var linkedUserName = person.UserId != null
            ? await _context.Users.Where(u => u.Id == person.UserId).Select(u => u.UserName).FirstOrDefaultAsync()
            : null;

        TenantMembershipStatus? membershipStatus = person.UserId != null
            ? (await _context.TenantMemberships.Where(m => m.UserId == person.UserId).Select(m => (TenantMembershipStatus?)m.Status).FirstOrDefaultAsync())
            : null;

        return MapToResult(person, linkedUserName, membershipStatus);
    }

    public async Task LinkUserAsync(int personId, string userId)
    {
        var person = await _context.Persons.FindAsync(personId)
            ?? throw new Exception(_localizer["PersonNotFound"]);

        var alreadyLinked = await _context.Persons.AnyAsync(p => p.UserId == userId && p.Id != personId);
        if (alreadyLinked)
            throw new Exception(_localizer["PersonAlreadyLinkedToAnotherAccount"]);

        person.UserId = userId;
        await _context.SaveChangesAsync();
    }

    public async Task UnlinkUserAsync(int personId)
    {
        var person = await _context.Persons.FindAsync(personId)
            ?? throw new Exception(_localizer["PersonNotFound"]);

        person.UserId = null;
        await _context.SaveChangesAsync();
    }

    public async Task<PersonResult> CreatePersonAsync(UpsertPersonRequest request)
    {
        var person = MapFromRequest(request);
        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
        return MapToResult(person);
    }

    public async Task UpdatePersonAsync(int personId, UpsertPersonRequest request)
    {
        var person = await _context.Persons.FindAsync(personId)
            ?? throw new Exception(_localizer["PersonNotFound"]);

        UpdateFromRequest(person, request);
        await _context.SaveChangesAsync();
    }

    private static PersonResult MapToResult(Person person, string? linkedUserName = null, TenantMembershipStatus? membershipStatus = null) => new()
    {
        Id = person.Id,
        FirstName = person.FirstName,
        LastName = person.LastName,
        Birthday = person.Birthday,
        DocumentType = person.DocumentType?.ToString(),
        DocumentId = person.DocumentId,
        Phone = person.Phone,
        PhoneExtension = person.PhoneExtension,
        Email = person.Email,
        Address = person.Address,
        PostalCode = person.PostalCode,
        Gender = person.Gender?.ToString(),
        AlternativePhone = person.AlternativePhone,
        CountryId = person.CountryId,
        StateId = person.StateId,
        CityId = person.CityId,
        CountryName = person.Country?.Name,
        StateName = person.State?.Name,
        CityName = person.City?.Name,
        LinkedUserName = linkedUserName,
        TenantId = person.TenantId,
        UserId = person.UserId,
        MembershipStatus = membershipStatus?.ToString()
    };

    private static Person MapFromRequest(UpsertPersonRequest request)
    {
        var person = new Person();
        UpdateFromRequest(person, request);
        return person;
    }

    private static void UpdateFromRequest(Person person, UpsertPersonRequest request)
    {
        person.FirstName = request.FirstName;
        person.LastName = request.LastName;
        person.Birthday = request.Birthday;
        person.DocumentType = string.IsNullOrEmpty(request.DocumentType)
            ? null : Enum.Parse<DocumentType>(request.DocumentType);
        person.DocumentId = string.IsNullOrEmpty(request.DocumentId) ? null : request.DocumentId;
        person.Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone;
        person.PhoneExtension = string.IsNullOrEmpty(request.PhoneExtension) ? null : request.PhoneExtension;
        person.Email = string.IsNullOrEmpty(request.Email) ? null : request.Email;
        person.Address = string.IsNullOrEmpty(request.Address) ? null : request.Address;
        person.PostalCode = string.IsNullOrEmpty(request.PostalCode) ? null : request.PostalCode;
        person.Gender = string.IsNullOrEmpty(request.Gender)
            ? null : Enum.Parse<Gender>(request.Gender);
        person.AlternativePhone = string.IsNullOrEmpty(request.AlternativePhone) ? null : request.AlternativePhone;
        person.CountryId = request.CountryId;
        person.StateId = request.StateId;
        person.CityId = request.CityId;
    }
}