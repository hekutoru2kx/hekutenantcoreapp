using Google.Apis.Auth;
using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly IStringLocalizer<Messages> _localizer;

    public GoogleTokenValidator(IConfiguration configuration, IStringLocalizer<Messages> localizer)
    {
        _configuration = configuration;
        _localizer = localizer;
    }

    public async Task<GoogleUserInfo> ValidateAsync(string idToken)
    {
        var clientId = _configuration["GoogleAuth:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
            throw new Exception(_localizer["GoogleAuthNotConfigured"]);

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            });
        }
        catch (InvalidJwtException)
        {
            throw new Exception(_localizer["InvalidGoogleToken"]);
        }

        return new GoogleUserInfo
        {
            Subject = payload.Subject,
            Email = payload.Email,
            Name = payload.Name,
            EmailVerified = payload.EmailVerified
        };
    }
}
