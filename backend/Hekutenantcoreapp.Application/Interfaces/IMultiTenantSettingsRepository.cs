using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IMultiTenantSettingsRepository
{
    Task<MultiTenantSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateMultiTenantSettingsRequest request);
}
