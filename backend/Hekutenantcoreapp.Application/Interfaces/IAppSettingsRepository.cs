using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IAppSettingsRepository
{
    Task<AppSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateAppSettingsRequest request);
}
