using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface ILoggingSettingsRepository
{
    Task<LoggingSettingsResult> GetSettingsAsync();
    Task UpdateSettingsAsync(UpdateLoggingSettingsRequest request);
}
