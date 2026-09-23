using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface ILoggingSettingsService
{
    Task<LoggingSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateLoggingSettingsRequest request);
}
