using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Serilog;

namespace Hekutenantcoreapp.Infrastructure.Logging;

public class CategoryLogger : ICategoryLogger
{
    private readonly CategoryLevelSwitches _switches;

    public CategoryLogger(CategoryLevelSwitches switches)
    {
        _switches = switches;
    }

    public void Log(LogCategory category, LogLevel level, string message, Exception? exception = null)
    {
        var serilogLevel = CategoryLevelSwitches.ToLogEventLevel(level);
        if (!_switches.IsEnabled(category, serilogLevel)) return;

        Serilog.Log.Logger
            .ForContext("LogCategory", category)
            .Write(serilogLevel, exception, message);
    }
}
