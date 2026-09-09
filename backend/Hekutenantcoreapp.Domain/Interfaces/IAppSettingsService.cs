using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IAppSettingsService
{
    Task<AppSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateAppSettingsRequest request);
}
