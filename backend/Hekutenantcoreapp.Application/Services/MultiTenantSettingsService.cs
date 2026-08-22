using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Application.Services;

public class MultiTenantSettingsService : IMultiTenantSettingsService
{
    private readonly IMultiTenantSettingsRepository _repository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IStringLocalizer<Messages> _localizer;

    public MultiTenantSettingsService(
        IMultiTenantSettingsRepository repository, ITenantRepository tenantRepository, IStringLocalizer<Messages> localizer)
    {
        _repository = repository;
        _tenantRepository = tenantRepository;
        _localizer = localizer;
    }

    public async Task<MultiTenantSettingsResult> GetSettingsAsync() =>
        await _repository.GetSettingsAsync();

    public async Task UpdateSettingsAsync(UpdateMultiTenantSettingsRequest request)
    {
        if (request.DefaultTenantLoginEnabled || request.MultiTenantDisabled)
        {
            if (!request.DefaultTenantId.HasValue)
                throw new Exception(_localizer["DefaultTenantRequiredForFlag"]);

            if (!await _tenantRepository.IsActiveTenantAsync(request.DefaultTenantId.Value))
                throw new Exception(_localizer["DefaultTenantMustBeActive"]);
        }

        await _repository.UpdateSettingsAsync(request);
    }
}
