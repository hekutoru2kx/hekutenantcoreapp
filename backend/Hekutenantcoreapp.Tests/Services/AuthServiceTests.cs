using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Hekutenantcoreapp.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IGoogleTokenValidator> _googleTokenValidator = new();
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<ITenantMembershipRepository> _tenantMembershipRepository = new();
    private readonly Mock<IUserTenantRoleRepository> _userTenantRoleRepository = new();
    private readonly Mock<IMultiTenantSettingsRepository> _multiTenantSettingsRepository = new();
    private readonly Mock<IStringLocalizer<Hekutenantcoreapp.Application.Resources.Messages>> _localizer = new();
    private readonly EmailTemplates _emailTemplates;

    public AuthServiceTests()
    {
        _localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _emailTemplates = new EmailTemplates(_localizer.Object);

        _tokenService.Setup(t => t.GenerateTokenAsync(It.IsAny<GenerateTokenRequest>()))
            .ReturnsAsync("fake-jwt");

        // Baseline: both flags off, no default tenant — matches today's pre-feature behavior
        // unless a test explicitly overrides this setup.
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult());
    }

    private AuthService CreateService() => new(
        _userRepository.Object,
        _tokenService.Object,
        _localizer.Object,
        _emailService.Object,
        _emailTemplates,
        _googleTokenValidator.Object,
        _tenantRepository.Object,
        _tenantMembershipRepository.Object,
        _userTenantRoleRepository.Object,
        _multiTenantSettingsRepository.Object);

    [Fact]
    public async Task SelectTenantAsync_Throws_For_NonMember_NonSuperAdmin()
    {
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetStatusAsync("user-1", 5))
            .ReturnsAsync((Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.SelectTenantAsync("user-1", 5));
    }

    [Fact]
    public async Task SelectTenantAsync_Throws_TenantAccessSuspended_When_Membership_Is_Suspended()
    {
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetStatusAsync("user-1", 5))
            .ReturnsAsync(Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus.Suspended);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SelectTenantAsync("user-1", 5));
        Assert.Equal("TenantAccessSuspended", ex.Message);
        _tenantMembershipRepository.Verify(r => r.ActivateAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task JoinTenantAsPatientAsync_Throws_TenantAccessSuspended_When_Already_Suspended_Member()
    {
        _tenantRepository.Setup(r => r.IsActiveTenantAsync(5)).ReturnsAsync(true);
        _tenantMembershipRepository.Setup(r => r.GetStatusAsync("user-1", 5))
            .ReturnsAsync(Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus.Suspended);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.JoinTenantAsPatientAsync("user-1", 5));
        Assert.Equal("TenantAccessSuspended", ex.Message);
        _tenantMembershipRepository.Verify(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus>()), Times.Never);
    }

    [Fact]
    public async Task JoinTenantAsPatientAsync_Throws_AlreadyTenantMember_When_Active_Member()
    {
        _tenantRepository.Setup(r => r.IsActiveTenantAsync(5)).ReturnsAsync(true);
        _tenantMembershipRepository.Setup(r => r.GetStatusAsync("user-1", 5))
            .ReturnsAsync(Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus.Active);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.JoinTenantAsPatientAsync("user-1", 5));
        Assert.Equal("AlreadyTenantMember", ex.Message);
    }

    [Fact]
    public async Task SelectTenantAsync_SuperAdmin_Can_Select_Tenant_With_No_Membership()
    {
        _userRepository.Setup(r => r.GetUserInfoAsync("super-1"))
            .ReturnsAsync(("super", (IList<string>)new List<string> { "SuperAdmin" }, false, "azure", (int?)null));
        _userRepository.Setup(r => r.GetProfileAsync("super-1"))
            .ReturnsAsync(new UserProfileResult
            {
                Id = "super-1",
                UserName = "super",
                Email = "super@x.com",
                PreferredLanguage = "en",
                PreferredTheme = "azure",
                Roles = new List<string> { "SuperAdmin" }
            });
        _tenantRepository.Setup(r => r.GetTenantByIdAsync(5))
            .ReturnsAsync(new TenantResult { Id = 5, Name = "Clinic B" });
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("super-1", 5))
            .ReturnsAsync(new List<string> { "Admin" });

        var service = CreateService();
        var result = await service.SelectTenantAsync("super-1", 5);

        _tenantMembershipRepository.Verify(r => r.HasActiveMembershipAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        Assert.Equal(5, result.TenantId);
        Assert.Contains("SuperAdmin", result.Roles);
        Assert.Contains("Admin", result.Roles);
    }

    [Fact]
    public async Task LoginAsync_AutoSelects_When_Exactly_One_Tenant_Available()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult> { new() { Id = 9, Name = "Only Clinic" } });
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("user-1", 9))
            .ReturnsAsync(new List<string> { "Nurse" });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Equal(9, result.TenantId);
        Assert.Equal("Only Clinic", result.TenantName);
        Assert.Empty(result.AvailableTenants);
    }

    [Fact]
    public async Task LoginAsync_Returns_TenantLess_Token_When_Multiple_Tenants_Available()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult>
            {
                new() { Id = 9, Name = "Clinic A" },
                new() { Id = 10, Name = "Clinic B" }
            });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Null(result.TenantId);
        Assert.Equal(2, result.AvailableTenants.Count);
    }

    [Fact]
    public async Task LoginAsync_Resolves_To_Personal_Default_When_Flag_Enabled()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)10));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult>
            {
                new() { Id = 9, Name = "Clinic A" },
                new() { Id = 10, Name = "Clinic B" }
            });
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult { DefaultTenantLoginEnabled = true });
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("user-1", 10))
            .ReturnsAsync(new List<string> { "Nurse" });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Equal(10, result.TenantId);
        Assert.Equal("Clinic B", result.TenantName);
    }

    [Fact]
    public async Task LoginAsync_Falls_Through_To_System_Default_When_Personal_Default_Is_Stale()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        // Personal default (99) points at a tenant the user is no longer a member of (e.g. it
        // was deactivated or membership was revoked) — availableTenants doesn't include it.
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)99));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult>
            {
                new() { Id = 9, Name = "Clinic A" },
                new() { Id = 10, Name = "Clinic B" }
            });
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult { DefaultTenantLoginEnabled = true, DefaultTenantId = 9 });
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("user-1", 9))
            .ReturnsAsync(new List<string> { "Nurse" });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Equal(9, result.TenantId);
        Assert.Equal("Clinic A", result.TenantName);
    }

    [Fact]
    public async Task LoginAsync_Falls_Through_To_Picker_When_No_Default_Resolves()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult>
            {
                new() { Id = 9, Name = "Clinic A" },
                new() { Id = 10, Name = "Clinic B" }
            });
        // Flag enabled, but neither a personal default nor a (valid) system default is set —
        // nothing to resolve to, so this must fall through to the picker rather than error.
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult { DefaultTenantLoginEnabled = true });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Null(result.TenantId);
        Assert.Equal(2, result.AvailableTenants.Count);
    }

    [Fact]
    public async Task LoginAsync_MultiTenantDisabled_Forces_Default_Resolution_Even_When_Flag_Off()
    {
        _userRepository.Setup(r => r.ValidateUserAsync("a@x.com", "pw")).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("user1", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _tenantMembershipRepository.Setup(r => r.GetMembershipTenantsForUserAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult>
            {
                new() { Id = 9, Name = "Clinic A" },
                new() { Id = 10, Name = "Clinic B" }
            });
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult { DefaultTenantLoginEnabled = false, MultiTenantDisabled = true, DefaultTenantId = 10 });
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("user-1", 10))
            .ReturnsAsync(new List<string> { "Nurse" });

        var service = CreateService();
        var result = await service.LoginAsync("a@x.com", "pw");

        Assert.Equal(10, result.TenantId);
        Assert.True(result.MultiTenantDisabled);
    }

    [Fact]
    public async Task RegisterAsync_Overrides_Requested_Tenant_With_System_Default_When_Effective_Flag_On()
    {
        _multiTenantSettingsRepository.Setup(r => r.GetSettingsAsync())
            .ReturnsAsync(new MultiTenantSettingsResult { MultiTenantDisabled = true, DefaultTenantId = 42 });
        _tenantRepository.Setup(r => r.IsActiveTenantAsync(42)).ReturnsAsync(true);
        _tenantRepository.Setup(r => r.GetTenantByIdAsync(42)).ReturnsAsync(new TenantResult { Id = 42, Name = "Default Clinic" });
        _userRepository.Setup(r => r.CreateUserAsync(It.IsAny<CreateUserRequest>())).ReturnsAsync("user-1");
        _userRepository.Setup(r => r.GetUserInfoAsync("user-1"))
            .ReturnsAsync(("newuser", (IList<string>)new List<string>(), false, "azure", (int?)null));
        _userTenantRoleRepository.Setup(r => r.GetRoleNamesAsync("user-1", 42))
            .ReturnsAsync(new List<string>());

        var service = CreateService();
        var result = await service.RegisterAsync(new RegisterRequest
        {
            UserName = "newuser",
            Email = "new@x.com",
            Password = "pw",
            TenantId = 7 // requested a different tenant — should be overridden
        });

        Assert.Equal(42, result.TenantId);
        Assert.Equal("Default Clinic", result.TenantName);
        _tenantMembershipRepository.Verify(r => r.CreateAsync("user-1", 42, Hekutenantcoreapp.Domain.Enums.TenantMembershipStatus.Active), Times.Once);
        _tenantRepository.Verify(r => r.IsActiveTenantAsync(7), Times.Never);
    }
}
