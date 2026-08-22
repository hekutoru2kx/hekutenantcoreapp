using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Hekutenantcoreapp.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IStringLocalizer<Hekutenantcoreapp.Application.Resources.Messages>> _localizer = new();
    private readonly EmailTemplates _emailTemplates;

    public UserServiceTests()
    {
        _localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _emailTemplates = new EmailTemplates(_localizer.Object);
    }

    private IUserService CreateService() =>
        new UserService(_userRepository.Object, _authService.Object, _emailService.Object, _emailTemplates, _localizer.Object);

    [Fact]
    public async Task UpdateProfileAsync_Throws_When_DefaultTenant_Is_Not_A_Membership()
    {
        _authService.Setup(a => a.GetAvailableTenantsAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult> { new() { Id = 9, Name = "Clinic A" } });

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateProfileAsync(new UpdateProfileRequest
        {
            UserId = "user-1",
            Email = "a@x.com",
            UserName = "user1",
            PreferredLanguage = "en",
            PreferredTheme = "azure",
            DefaultTenantId = 99
        }));

        Assert.Equal("DefaultTenantNotAMembership", ex.Message);
        _userRepository.Verify(r => r.UpdateProfileAsync(It.IsAny<UpdateProfileRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateProfileAsync_Saves_When_DefaultTenant_Is_A_Membership()
    {
        _authService.Setup(a => a.GetAvailableTenantsAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult> { new() { Id = 9, Name = "Clinic A" } });

        var service = CreateService();
        await service.UpdateProfileAsync(new UpdateProfileRequest
        {
            UserId = "user-1",
            Email = "a@x.com",
            UserName = "user1",
            PreferredLanguage = "en",
            PreferredTheme = "azure",
            DefaultTenantId = 9
        });

        _userRepository.Verify(r => r.UpdateProfileAsync(It.Is<UpdateProfileRequest>(req => req.DefaultTenantId == 9)), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_Skips_Membership_Check_When_Clearing_The_Default()
    {
        var service = CreateService();
        await service.UpdateProfileAsync(new UpdateProfileRequest
        {
            UserId = "user-1",
            Email = "a@x.com",
            UserName = "user1",
            PreferredLanguage = "en",
            PreferredTheme = "azure",
            DefaultTenantId = null
        });

        _authService.Verify(a => a.GetAvailableTenantsAsync(It.IsAny<string>()), Times.Never);
        _userRepository.Verify(r => r.UpdateProfileAsync(It.IsAny<UpdateProfileRequest>()), Times.Once);
    }
}
