using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Application.Resources;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
{
    private readonly IStringLocalizer<Messages> _localizer;

    public LocalizedIdentityErrorDescriber(IStringLocalizer<Messages> localizer)
    {
        _localizer = localizer;
    }

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = string.Format(_localizer["PasswordTooShort"], length) };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = _localizer["PasswordRequiresUpper"] };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = _localizer["PasswordRequiresLower"] };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = _localizer["PasswordRequiresDigit"] };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = _localizer["PasswordRequiresNonAlphanumeric"] };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = _localizer["DuplicateEmail"] };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = _localizer["DuplicateUserName"] };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = _localizer["InvalidEmail"] };

    
}