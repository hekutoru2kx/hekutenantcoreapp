using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Services;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;

namespace Hekutenantcoreapp.Tests.Services;

public class MultiTenantSettingsServiceTests
{
    private readonly Mock<IMultiTenantSettingsRepository> _repository = new();
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<IStringLocalizer<Hekutenantcoreapp.Application.Resources.Messages>> _localizer = new();

    public MultiTenantSettingsServiceTests()
    {
        _localizer.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
    }

    private IMultiTenantSettingsService CreateService() =>
        new MultiTenantSettingsService(_repository.Object, _tenantRepository.Object, _localizer.Object);

    [Fact]
    public async Task UpdateSettingsAsync_Throws_When_Enabling_DefaultTenantLogin_Without_DefaultTenant()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateSettingsAsync(new UpdateMultiTenantSettingsRequest
        {
            DefaultTenantLoginEnabled = true,
            DefaultTenantId = null
        }));

        Assert.Equal("DefaultTenantRequiredForFlag", ex.Message);
        _repository.Verify(r => r.UpdateSettingsAsync(It.IsAny<UpdateMultiTenantSettingsRequest>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Throws_When_Enabling_MultiTenantDisabled_Without_DefaultTenant()
    {
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateSettingsAsync(new UpdateMultiTenantSettingsRequest
        {
            MultiTenantDisabled = true,
            DefaultTenantId = null
        }));

        Assert.Equal("DefaultTenantRequiredForFlag", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Throws_When_DefaultTenant_Is_Not_Active()
    {
        _tenantRepository.Setup(r => r.IsActiveTenantAsync(5)).ReturnsAsync(false);
        var service = CreateService();

        var ex = await Assert.ThrowsAsync<Exception>(() => service.UpdateSettingsAsync(new UpdateMultiTenantSettingsRequest
        {
            DefaultTenantLoginEnabled = true,
            DefaultTenantId = 5
        }));

        Assert.Equal("DefaultTenantMustBeActive", ex.Message);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Succeeds_When_Both_Flags_Off_Even_Without_DefaultTenant()
    {
        var service = CreateService();

        await service.UpdateSettingsAsync(new UpdateMultiTenantSettingsRequest
        {
            DefaultTenantLoginEnabled = false,
            MultiTenantDisabled = false,
            DefaultTenantId = null
        });

        _repository.Verify(r => r.UpdateSettingsAsync(It.IsAny<UpdateMultiTenantSettingsRequest>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSettingsAsync_Succeeds_When_Flag_On_And_DefaultTenant_Is_Active()
    {
        _tenantRepository.Setup(r => r.IsActiveTenantAsync(5)).ReturnsAsync(true);
        var service = CreateService();

        await service.UpdateSettingsAsync(new UpdateMultiTenantSettingsRequest
        {
            DefaultTenantLoginEnabled = true,
            DefaultTenantId = 5
        });

        _repository.Verify(r => r.UpdateSettingsAsync(It.IsAny<UpdateMultiTenantSettingsRequest>()), Times.Once);
    }
}
