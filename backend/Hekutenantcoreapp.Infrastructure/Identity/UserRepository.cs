using Hekutenantcoreapp.Application.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Messages> _localizer;

    private readonly HekutenantcoreappDbContext _context;

    public UserRepository(UserManager<ApplicationUser> userManager, IStringLocalizer<Messages> localizer, HekutenantcoreappDbContext context)
    {
        _userManager = userManager;
        _localizer = localizer;
        _context = context;
    }

    public async Task<string> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        return user.Id;
    }

    public async Task<string?> ValidateUserAsync(string usernameOrEmail, string password)
    {
        var user = await _userManager.FindByEmailAsync(usernameOrEmail)
        ?? await _userManager.FindByNameAsync(usernameOrEmail);

        if (user == null || !await _userManager.CheckPasswordAsync(user, password))
            return null;

        return user.Id;
    }

    public async Task<string?> FindUserIdByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user?.Id;
    }

    public async Task AssignRoleAsync(string email, string role)
    {
        var user = await _userManager.FindByEmailAsync(email)
            ?? throw new Exception(_localizer["UserNotFound"]);

        await _userManager.AddToRoleAsync(user, role);
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);
        return await _userManager.GetRolesAsync(user);
    }

    public async Task UpdateLanguageAsync(string userId, string language)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        user.PreferredLanguage = language;
        await _userManager.UpdateAsync(user);
    }

    public async Task<(string UserName, IList<string> Roles, bool MustChangePassword, string PreferredTheme, int? DefaultTenantId)> GetUserInfoAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        var roles = await _userManager.GetRolesAsync(user);
        return (user.UserName, roles, user.MustChangePassword, user.PreferredTheme, user.DefaultTenantId);
    }

    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);
    }

    public async Task<UserProfileResult> GetProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        var roles = await _userManager.GetRolesAsync(user);

        return new UserProfileResult
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PreferredLanguage = user.PreferredLanguage,
            PreferredTheme = user.PreferredTheme,
            Roles = roles,
            DefaultTenantId = user.DefaultTenantId
        };
    }

    public async Task UpdateProfileAsync(UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        user.Email = request.Email;
        user.PreferredLanguage = request.PreferredLanguage;
        user.PreferredTheme = request.PreferredTheme;
        user.DefaultTenantId = request.DefaultTenantId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task<PersonResult?> GetPersonAsync(string userId)
{
    // Scoped automatically to the caller's current tenant by HekutenantcoreappDbContext's
    // ITenantScoped query filter — "my linked person record" means "in the tenant I'm in now".
    var person = await _context.Persons.FirstOrDefaultAsync(p => p.UserId == userId);
    if (person == null) return null;

    return new PersonResult
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
        CityId = person.CityId
    };
}

public async Task UpsertPersonAsync(string userId, UpsertPersonRequest request)
{
    var user = await _userManager.FindByIdAsync(userId)
        ?? throw new Exception(_localizer["UserNotFound"]);

    var existingLinkedPerson = await _context.Persons.FirstOrDefaultAsync(p => p.UserId == userId);

    if (existingLinkedPerson == null)
    {
        Person? existingPerson = null;
        if (!string.IsNullOrEmpty(request.DocumentType) && !string.IsNullOrEmpty(request.DocumentId))
        {
            var documentType = Enum.Parse<DocumentType>(request.DocumentType);
            existingPerson = await _context.Persons.FirstOrDefaultAsync(p =>
                p.DocumentType == documentType && p.DocumentId == request.DocumentId);
        }

        if (existingPerson != null)
        {
            var emailMatches = !string.IsNullOrEmpty(existingPerson.Email)
                && !string.IsNullOrEmpty(request.Email)
                && string.Equals(existingPerson.Email, request.Email, StringComparison.OrdinalIgnoreCase);

            if (existingPerson.UserId != null || !emailMatches)
                throw new Exception(_localizer["PersonAlreadyExistsCannotLink"]);

            existingPerson.UserId = userId;
            await _context.SaveChangesAsync();
            return;
        }

        var person = new Person
        {
            UserId = userId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Birthday = request.Birthday,
            DocumentType = request.DocumentType != null ? Enum.Parse<DocumentType>(request.DocumentType) : null,
            DocumentId = string.IsNullOrEmpty(request.DocumentId) ? null : request.DocumentId,
            Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone,
            PhoneExtension = string.IsNullOrEmpty(request.PhoneExtension) ? null : request.PhoneExtension,
            Email = string.IsNullOrEmpty(request.Email) ? null : request.Email,
            Address = string.IsNullOrEmpty(request.Address) ? null : request.Address,
            PostalCode = string.IsNullOrEmpty(request.PostalCode) ? null : request.PostalCode,
            Gender = request.Gender != null ? Enum.Parse<Gender>(request.Gender) : null,
            AlternativePhone = string.IsNullOrEmpty(request.AlternativePhone) ? null : request.AlternativePhone,
            CountryId = request.CountryId,
            StateId = request.StateId,
            CityId = request.CityId
        };

        _context.Persons.Add(person);
        await _context.SaveChangesAsync();
    }
    else
    {
        existingLinkedPerson.FirstName = request.FirstName;
        existingLinkedPerson.LastName = request.LastName;
        existingLinkedPerson.Birthday = request.Birthday;
        existingLinkedPerson.DocumentType = request.DocumentType != null ? Enum.Parse<DocumentType>(request.DocumentType) : null;
        existingLinkedPerson.DocumentId = string.IsNullOrEmpty(request.DocumentId) ? null : request.DocumentId;
        existingLinkedPerson.Phone = string.IsNullOrEmpty(request.Phone) ? null : request.Phone;
        existingLinkedPerson.PhoneExtension = string.IsNullOrEmpty(request.PhoneExtension) ? null : request.PhoneExtension;
        existingLinkedPerson.Email = string.IsNullOrEmpty(request.Email) ? null : request.Email;
        existingLinkedPerson.Address = string.IsNullOrEmpty(request.Address) ? null : request.Address;
        existingLinkedPerson.PostalCode = string.IsNullOrEmpty(request.PostalCode) ? null : request.PostalCode;
        existingLinkedPerson.Gender = request.Gender != null ? Enum.Parse<Gender>(request.Gender) : null;
        existingLinkedPerson.AlternativePhone = string.IsNullOrEmpty(request.AlternativePhone) ? null : request.AlternativePhone;
        existingLinkedPerson.CountryId = request.CountryId;
        existingLinkedPerson.StateId = request.StateId;
        existingLinkedPerson.CityId = request.CityId;

        await _context.SaveChangesAsync();
    }
}

// Read-only preview of what UpsertPersonAsync's matching branch would do — lets the
// profile form warn "this will link to an existing record" (or "contact an administrator")
// before the user submits, instead of only finding out from the save's error message.
public async Task<PersonMatchResult> CheckExistingPersonAsync(string callerId, string? documentType, string? documentId, string? email)
{
    if (string.IsNullOrEmpty(documentType) || string.IsNullOrEmpty(documentId))
        return new PersonMatchResult { MatchFound = false };

    var parsedType = Enum.Parse<DocumentType>(documentType);
    var existingPerson = await _context.Persons.FirstOrDefaultAsync(p =>
        p.DocumentType == parsedType && p.DocumentId == documentId);

    // No match, or the match is the caller's own already-linked record — nothing to report,
    // since editing your own profile with your own unchanged document isn't "linking".
    if (existingPerson == null || existingPerson.UserId == callerId)
        return new PersonMatchResult { MatchFound = false };

    var emailMatches = !string.IsNullOrEmpty(existingPerson.Email)
        && !string.IsNullOrEmpty(email)
        && string.Equals(existingPerson.Email, email, StringComparison.OrdinalIgnoreCase);

    return new PersonMatchResult
    {
        MatchFound = true,
        Linkable = existingPerson.UserId == null && emailMatches
    };
}

public async Task<(string UserId, bool IsNewUser)> FindOrCreateGoogleUserAsync(GoogleUserInfo googleUser)
{
    const string provider = "Google";

    var user = await _userManager.FindByLoginAsync(provider, googleUser.Subject);
    if (user != null) return (user.Id, false);

    user = await _userManager.FindByEmailAsync(googleUser.Email);
    if (user != null)
    {
        var linkResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, googleUser.Subject, provider));
        if (!linkResult.Succeeded)
            throw new Exception(string.Join(", ", linkResult.Errors.Select(e => e.Description)));

        return (user.Id, false);
    }

    var normalizedEmail = _userManager.NormalizeEmail(googleUser.Email);
    var wasDeleted = await _context.DeletedAccounts.AnyAsync(d => d.NormalizedEmail == normalizedEmail);
    if (wasDeleted)
        throw new Exception(_localizer["AccountWasDeleted"]);

    var baseUserName = googleUser.Email;
    var userName = baseUserName;
    var suffix = 1;
    while (await _userManager.FindByNameAsync(userName) != null)
        userName = $"{baseUserName}{suffix++}";

    var newUser = new ApplicationUser
    {
        UserName = userName,
        Email = googleUser.Email,
        EmailConfirmed = true
    };

    var createResult = await _userManager.CreateAsync(newUser);
    if (!createResult.Succeeded)
        throw new Exception(string.Join(", ", createResult.Errors.Select(e => e.Description)));

    var addLoginResult = await _userManager.AddLoginAsync(newUser, new UserLoginInfo(provider, googleUser.Subject, provider));
    if (!addLoginResult.Succeeded)
        throw new Exception(string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));

    return (newUser.Id, true);
}
}