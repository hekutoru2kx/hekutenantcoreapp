using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Application.Resources;

namespace Hekutenantcoreapp.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly IEmailService _emailService;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMembershipRepository _tenantMembershipRepository;
    private readonly IUserTenantRoleRepository _userTenantRoleRepository;
    private readonly IMultiTenantSettingsRepository _multiTenantSettingsRepository;

    private readonly EmailTemplates _emailTemplates;

    public AuthService(
        IUserRepository userRepository,
        ITokenService tokenService,
        IStringLocalizer<Messages> localizer,
        IEmailService emailService,
        EmailTemplates emailTemplates,
        IGoogleTokenValidator googleTokenValidator,
        ITenantRepository tenantRepository,
        ITenantMembershipRepository tenantMembershipRepository,
        IUserTenantRoleRepository userTenantRoleRepository,
        IMultiTenantSettingsRepository multiTenantSettingsRepository)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _localizer = localizer;
        _emailService = emailService;
        _googleTokenValidator = googleTokenValidator;
        _tenantRepository = tenantRepository;
        _tenantMembershipRepository = tenantMembershipRepository;
        _userTenantRoleRepository = userTenantRoleRepository;
        _multiTenantSettingsRepository = multiTenantSettingsRepository;
        _emailTemplates = emailTemplates;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // When the effective flag is on, the tenant dropdown is hidden client-side and the
        // system default tenant is used regardless of whatever the request carries.
        var settings = await _multiTenantSettingsRepository.GetSettingsAsync();
        var tenantId = (settings.DefaultTenantLoginEnabled || settings.MultiTenantDisabled) && settings.DefaultTenantId.HasValue
            ? settings.DefaultTenantId.Value
            : request.TenantId;

        if (!await _tenantRepository.IsActiveTenantAsync(tenantId))
            throw new Exception(_localizer["TenantNotActive"]);

        var userId = await _userRepository.CreateUserAsync(new CreateUserRequest
        {
            UserName = request.UserName,
            Email = request.Email,
            Password = request.Password
        });

        await _tenantMembershipRepository.CreateAsync(userId, tenantId, TenantMembershipStatus.Active);

        var (_, roles, mustChangePassword, preferredTheme, _) = await _userRepository.GetUserInfoAsync(userId);
        var tenant = await _tenantRepository.GetTenantByIdAsync(tenantId);
        var result = await MintTenantScopedTokenAsync(
            userId, request.Email, request.UserName, roles.Contains("SuperAdmin"),
            mustChangePassword, preferredTheme, tenantId, tenant?.Name);

        var (subject, body) = _emailTemplates.Welcome(request.UserName);
        await _emailService.SendAsync(request.Email, subject, body);

        return result;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var userId = await _userRepository.ValidateUserAsync(email, password)
            ?? throw new Exception(_localizer["InvalidCredentials"]);

        var (userName, roles, mustChangePassword, preferredTheme, defaultTenantId) = await _userRepository.GetUserInfoAsync(userId);
        return await ResolveAndMintAsync(userId, email, userName, roles.Contains("SuperAdmin"), mustChangePassword, preferredTheme, defaultTenantId);
    }

    public async Task AssignRoleAsync(string email, string role)
    {
        await _userRepository.AssignRoleAsync(email, role);
    }

    public async Task<AuthResult> LoginOrRegisterWithGoogleAsync(string idToken, int? tenantId)
    {
        var googleUser = await _googleTokenValidator.ValidateAsync(idToken);

        if (!googleUser.EmailVerified)
            throw new Exception(_localizer["GoogleEmailNotVerified"]);

        var (userId, isNewUser) = await _userRepository.FindOrCreateGoogleUserAsync(googleUser);

        if (isNewUser && tenantId.HasValue)
            await _tenantMembershipRepository.CreateAsync(userId, tenantId.Value, TenantMembershipStatus.Active);

        var (userName, roles, mustChangePassword, preferredTheme, defaultTenantId) = await _userRepository.GetUserInfoAsync(userId);
        var isSuperAdmin = roles.Contains("SuperAdmin");

        AuthResult result;
        if (isNewUser && tenantId.HasValue)
        {
            var tenant = await _tenantRepository.GetTenantByIdAsync(tenantId.Value);
            result = await MintTenantScopedTokenAsync(userId, googleUser.Email, userName, isSuperAdmin, mustChangePassword, preferredTheme, tenantId, tenant?.Name);
        }
        else
        {
            result = await ResolveAndMintAsync(userId, googleUser.Email, userName, isSuperAdmin, mustChangePassword, preferredTheme, defaultTenantId);
        }

        if (isNewUser)
        {
            var (subject, body) = _emailTemplates.Welcome(userName);
            await _emailService.SendAsync(googleUser.Email, subject, body);
        }

        return result;
    }

    public async Task<AuthResult> SelectTenantAsync(string userId, int tenantId)
    {
        var (userName, roles, mustChangePassword, preferredTheme, _) = await _userRepository.GetUserInfoAsync(userId);
        var isSuperAdmin = roles.Contains("SuperAdmin");

        if (!isSuperAdmin)
        {
            var status = await _tenantMembershipRepository.GetStatusAsync(userId, tenantId);
            if (status == TenantMembershipStatus.Suspended)
                throw new Exception(_localizer["TenantAccessSuspended"]);
            if (status == null)
                throw new Exception(_localizer["NotTenantMember"]);

            await _tenantMembershipRepository.ActivateAsync(userId, tenantId);
        }

        var profile = await _userRepository.GetProfileAsync(userId);
        var tenant = await _tenantRepository.GetTenantByIdAsync(tenantId);
        return await MintTenantScopedTokenAsync(userId, profile.Email, userName, isSuperAdmin, mustChangePassword, preferredTheme, tenantId, tenant?.Name);
    }

    public async Task<AuthResult> JoinTenantAsPatientAsync(string userId, int tenantId)
    {
        if (!await _tenantRepository.IsActiveTenantAsync(tenantId))
            throw new Exception(_localizer["TenantNotActive"]);

        var existingStatus = await _tenantMembershipRepository.GetStatusAsync(userId, tenantId);
        if (existingStatus == TenantMembershipStatus.Suspended)
            throw new Exception(_localizer["TenantAccessSuspended"]);
        if (existingStatus != null)
            throw new Exception(_localizer["AlreadyTenantMember"]);

        await _tenantMembershipRepository.CreateAsync(userId, tenantId, TenantMembershipStatus.Active);

        var (userName, roles, mustChangePassword, preferredTheme, _) = await _userRepository.GetUserInfoAsync(userId);
        var profile = await _userRepository.GetProfileAsync(userId);
        var tenant = await _tenantRepository.GetTenantByIdAsync(tenantId);
        return await MintTenantScopedTokenAsync(userId, profile.Email, userName, roles.Contains("SuperAdmin"), mustChangePassword, preferredTheme, tenantId, tenant?.Name);
    }

    public async Task<IList<TenantSummaryResult>> GetAvailableTenantsAsync(string userId)
    {
        var (_, roles, _, _, _) = await _userRepository.GetUserInfoAsync(userId);
        if (roles.Contains("SuperAdmin"))
            return await _tenantRepository.GetActiveTenantSummariesAsync();

        return await _tenantMembershipRepository.GetMembershipTenantsForUserAsync(userId);
    }

    // Resolves straight to a tenant when there's only one candidate, or when the effective
    // default-login setting (DefaultTenantLoginEnabled, forced on by MultiTenantDisabled) can
    // resolve one via the user's personal default or the system-wide default. Both defaults are
    // only honored if they're still in availableTenants — which already filters to active
    // tenants and non-suspended memberships — so a stale default pointing at a tenant the user
    // lost access to, or that was deactivated, is silently skipped rather than failing. Anything
    // left unresolved falls through to a tenant-less token carrying the candidate list, i.e.
    // today's picker flow — the safety valve that means nobody is ever fully locked out.
    private async Task<AuthResult> ResolveAndMintAsync(
        string userId, string email, string userName, bool isSuperAdmin, bool mustChangePassword, string preferredTheme,
        int? personalDefaultTenantId)
    {
        var availableTenants = isSuperAdmin
            ? await _tenantRepository.GetActiveTenantSummariesAsync()
            : await _tenantMembershipRepository.GetMembershipTenantsForUserAsync(userId);

        if (availableTenants.Count == 1)
        {
            return await MintTenantScopedTokenAsync(
                userId, email, userName, isSuperAdmin, mustChangePassword, preferredTheme,
                availableTenants[0].Id, availableTenants[0].Name);
        }

        var settings = await _multiTenantSettingsRepository.GetSettingsAsync();
        if (settings.DefaultTenantLoginEnabled || settings.MultiTenantDisabled)
        {
            var resolved =
                (personalDefaultTenantId.HasValue ? availableTenants.FirstOrDefault(t => t.Id == personalDefaultTenantId.Value) : null)
                ?? (settings.DefaultTenantId.HasValue ? availableTenants.FirstOrDefault(t => t.Id == settings.DefaultTenantId.Value) : null);

            if (resolved != null)
            {
                return await MintTenantScopedTokenAsync(
                    userId, email, userName, isSuperAdmin, mustChangePassword, preferredTheme,
                    resolved.Id, resolved.Name);
            }
        }

        return await MintTenantScopedTokenAsync(
            userId, email, userName, isSuperAdmin, mustChangePassword, preferredTheme,
            null, null, availableTenants);
    }

    private async Task<AuthResult> MintTenantScopedTokenAsync(
        string userId, string email, string userName, bool isSuperAdmin, bool mustChangePassword, string preferredTheme,
        int? tenantId, string? tenantName = null, IList<TenantSummaryResult>? availableTenants = null)
    {
        var roleNames = new List<string>();
        if (isSuperAdmin) roleNames.Add("SuperAdmin");
        if (tenantId.HasValue)
            roleNames.AddRange(await _userTenantRoleRepository.GetRoleNamesAsync(userId, tenantId.Value));
        roleNames = roleNames.Distinct().ToList();

        var token = await _tokenService.GenerateTokenAsync(new GenerateTokenRequest
        {
            UserId = userId,
            Email = email,
            UserName = userName,
            Roles = roleNames,
            TenantId = tenantId
        });

        var settings = await _multiTenantSettingsRepository.GetSettingsAsync();

        return new AuthResult
        {
            Token = token,
            UserId = userId,
            Email = email,
            UserName = userName,
            Roles = roleNames,
            MustChangePassword = mustChangePassword,
            PreferredTheme = preferredTheme,
            TenantId = tenantId,
            TenantName = tenantName,
            AvailableTenants = availableTenants ?? new List<TenantSummaryResult>(),
            MultiTenantDisabled = settings.MultiTenantDisabled
        };
    }
}
