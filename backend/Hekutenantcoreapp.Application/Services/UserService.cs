using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly EmailTemplates _emailTemplates;
    private readonly IStringLocalizer<Messages> _localizer;

    public UserService(
        IUserRepository userRepository, IAuthService authService, IEmailService emailService,
        EmailTemplates emailTemplates, IStringLocalizer<Messages> localizer)
    {
        _userRepository = userRepository;
        _authService = authService;
        _emailService = emailService;
        _emailTemplates = emailTemplates;
        _localizer = localizer;
    }

    public async Task UpdateLanguageAsync(string userId, string language)
    {
        await _userRepository.UpdateLanguageAsync(userId, language);
    }

    public async Task<UserProfileResult> GetProfileAsync(string userId)
    {
        return await _userRepository.GetProfileAsync(userId);
    }
    public async Task UpdateProfileAsync(UpdateProfileRequest request)
    {
        if (request.DefaultTenantId.HasValue)
        {
            var availableTenants = await _authService.GetAvailableTenantsAsync(request.UserId);
            if (!availableTenants.Any(t => t.Id == request.DefaultTenantId.Value))
                throw new Exception(_localizer["DefaultTenantNotAMembership"]);
        }

        await _userRepository.UpdateProfileAsync(request);
    }
    public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        await _userRepository.ChangePasswordAsync(userId, currentPassword, newPassword);

        var profile = await _userRepository.GetProfileAsync(userId);
        var (subject, body) = _emailTemplates.PasswordChanged(profile.UserName);
        await _emailService.SendAsync(profile.Email, subject, body);
    }

    public async Task<PersonResult?> GetPersonAsync(string userId) =>
        await _userRepository.GetPersonAsync(userId);

    public async Task UpsertPersonAsync(string userId, UpsertPersonRequest request) =>
        await _userRepository.UpsertPersonAsync(userId, request);

    public async Task<PersonMatchResult> CheckExistingPersonAsync(string callerId, string? documentType, string? documentId, string? email) =>
        await _userRepository.CheckExistingPersonAsync(callerId, documentType, documentId, email);
}