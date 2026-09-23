using Hekutenantcoreapp.Domain.Enums;
using Serilog.Core;
using Serilog.Events;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// One live-adjustable LoggingLevelSwitch per LogCategory, shared between CategoryLogger (which
// gates every categorized log call against it) and LoggingSettingsRefreshHostedService (which
// pushes SuperAdmin's saved minimum levels into it on a timer) — a change takes effect on the
// next log call, no restart needed. Registered as a DI singleton.
public class CategoryLevelSwitches
{
    private readonly Dictionary<LogCategory, LoggingLevelSwitch> _switches =
        Enum.GetValues<LogCategory>().ToDictionary(c => c, _ => new LoggingLevelSwitch(LogEventLevel.Information));

    public void SetMinimumLevel(LogCategory category, LogLevel level) =>
        _switches[category].MinimumLevel = ToLogEventLevel(level);

    public bool IsEnabled(LogCategory category, LogEventLevel level) => level >= _switches[category].MinimumLevel;

    public static LogEventLevel ToLogEventLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        LogLevel.None => LevelAlias.Off,
        _ => LogEventLevel.Information
    };
}
