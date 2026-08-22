using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IDatabaseKeepAliveRepository
{
    Task<DatabaseKeepAliveSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateDatabaseKeepAliveSettingsRequest request);
}
