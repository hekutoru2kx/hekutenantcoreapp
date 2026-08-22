using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserManagementRepository _repository;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly EmailTemplates _emailTemplates;
    private readonly IStringLocalizer<Messages> _localizer;

    public UserManagementService(
        IUserManagementRepository repository, IAuthService authService, IEmailService emailService,
        EmailTemplates emailTemplates, IStringLocalizer<Messages> localizer)
    {
        _repository = repository;
        _authService = authService;
        _emailService = emailService;
        _emailTemplates = emailTemplates;
        _localizer = localizer;
    }

    public async Task<PagedResult<UserListResult>> GetUsersAsync(UserListQuery query) =>
        await _repository.GetUsersAsync(query);

    public async Task<IList<UserListResult>> GetAllUsersAsync(string? search, string? sortBy, string? sortDirection, string? roleFilter, bool? statusFilter) =>
        await _repository.GetAllUsersAsync(search, sortBy, sortDirection, roleFilter, statusFilter);

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request) =>
    await _repository.CreateUserAsync(request);

    public async Task GrantSuperAdminAsync(string userId) =>
        await _repository.GrantSuperAdminAsync(userId);

    public async Task RevokeSuperAdminAsync(string userId) =>
        await _repository.RevokeSuperAdminAsync(userId);

    public async Task DeactivateUserAsync(string userId) =>
        await _repository.DeactivateUserAsync(userId);

    public async Task ActivateUserAsync(string userId) =>
        await _repository.ActivateUserAsync(userId);

    public async Task ResetPasswordAsync(string userId, string newPassword)
    {
        await _repository.ResetPasswordAsync(userId, newPassword);

        var user = await _repository.GetUserByIdAsync(userId);
        if (user != null)
        {
            var (subject, body) = _emailTemplates.PasswordReset(
                user.UserName,
                newPassword);
            await _emailService.SendAsync(user.Email, subject, body);
        }
    }

    public async Task<UserListResult?> GetUserByIdAsync(string userId) =>
        await _repository.GetUserByIdAsync(userId);
    public async Task DeleteUserAsync(string userId) =>
        await _repository.DeleteUserAsync(userId);

    public async Task<IList<TenantSummaryResult>> GetUserTenantsAsync(string userId) =>
        await _authService.GetAvailableTenantsAsync(userId);

    public async Task SetDefaultTenantAsync(string userId, int? tenantId)
    {
        if (tenantId.HasValue)
        {
            var availableTenants = await _authService.GetAvailableTenantsAsync(userId);
            if (!availableTenants.Any(t => t.Id == tenantId.Value))
                throw new Exception(_localizer["DefaultTenantNotAMembership"]);
        }

        await _repository.SetDefaultTenantAsync(userId, tenantId);
    }
}