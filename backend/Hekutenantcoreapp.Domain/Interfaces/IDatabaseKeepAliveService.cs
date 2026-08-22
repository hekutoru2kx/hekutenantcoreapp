using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IDatabaseKeepAliveService
{
    Task<DatabaseKeepAliveSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateDatabaseKeepAliveSettingsRequest request);
}
