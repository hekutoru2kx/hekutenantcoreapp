using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Application.Interfaces;

// The one entry point Application/Infrastructure code uses to emit a categorized log line (Http/
// Database/Security/Integration/Business/System). Kept in Application (not Infrastructure) so
// Application-layer services can take a dependency on it without Application depending on
// Infrastructure.
public interface ICategoryLogger
{
    void Log(LogCategory category, LogLevel level, string message, Exception? exception = null);
}

public static class CategoryLoggerExtensions
{
    // Convenience for call sites (e.g. controllers) that only ever log caught exceptions at Error
    // — also sidesteps having to name Hekutenantcoreapp.Domain.Enums.LogLevel directly in a
    // project like Hekutenantcoreapp.Api where Microsoft.Extensions.Logging.LogLevel is an
    // implicit using and the two would collide.
    public static void LogError(this ICategoryLogger logger, LogCategory category, string message, Exception exception) =>
        logger.Log(category, LogLevel.Error, message, exception);
}
