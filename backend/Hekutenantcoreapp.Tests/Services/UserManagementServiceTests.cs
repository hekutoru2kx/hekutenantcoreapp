using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Hekutenantcoreapp.Tests.Services;

public class UserManagementServiceTests
{
    private readonly Mock<IUserManagementRepository> _repository = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IStringLocalizer<Hekutenantcoreapp.Application.Resources.Messages>> _localizer = new();
    private readonly EmailTemplates _emailTemplates;

    public UserManagementServiceTests()
    {
        _localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        _emailTemplates = new EmailTemplates(_localizer.Object);
    }

    private IUserManagementService CreateService() =>
        new UserManagementService(_repository.Object, _authService.Object, _emailService.Object, _emailTemplates, _localizer.Object);

    [Fact]
    public async Task SetDefaultTenantAsync_Throws_When_Tenant_Is_Not_A_Membership()
    {
        _authService.Setup(a => a.GetAvailableTenantsAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult> { new() { Id = 9, Name = "Clinic A" } });

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SetDefaultTenantAsync("user-1", 99));

        Assert.Equal("DefaultTenantNotAMembership", ex.Message);
        _repository.Verify(r => r.SetDefaultTenantAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultTenantAsync_Persists_When_Tenant_Is_A_Membership()
    {
        _authService.Setup(a => a.GetAvailableTenantsAsync("user-1"))
            .ReturnsAsync(new List<TenantSummaryResult> { new() { Id = 9, Name = "Clinic A" } });

        var service = CreateService();
        await service.SetDefaultTenantAsync("user-1", 9);

        _repository.Verify(r => r.SetDefaultTenantAsync("user-1", 9), Times.Once);
    }

    [Fact]
    public async Task SetDefaultTenantAsync_Allows_Clearing_The_Default_Without_Membership_Check()
    {
        var service = CreateService();
        await service.SetDefaultTenantAsync("user-1", null);

        _repository.Verify(r => r.SetDefaultTenantAsync("user-1", null), Times.Once);
        _authService.Verify(a => a.GetAvailableTenantsAsync(It.IsAny<string>()), Times.Never);
    }
}
