using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IMultiTenantSettingsService
{
    Task<MultiTenantSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateMultiTenantSettingsRequest request);
}
