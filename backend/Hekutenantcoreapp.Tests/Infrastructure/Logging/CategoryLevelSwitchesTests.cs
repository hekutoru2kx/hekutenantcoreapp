using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Infrastructure.Logging;
using Serilog.Events;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure.Logging;

public class CategoryLevelSwitchesTests
{
    [Fact]
    public void IsEnabled_DefaultsToInformation_ForEveryCategory()
    {
        var switches = new CategoryLevelSwitches();

        foreach (var category in Enum.GetValues<LogCategory>())
        {
            Assert.False(switches.IsEnabled(category, LogEventLevel.Debug));
            Assert.True(switches.IsEnabled(category, LogEventLevel.Information));
        }
    }

    [Fact]
    public void SetMinimumLevel_ChangesGatingForThatCategoryOnly()
    {
        var switches = new CategoryLevelSwitches();

        switches.SetMinimumLevel(LogCategory.Database, LogLevel.Debug);

        Assert.True(switches.IsEnabled(LogCategory.Database, LogEventLevel.Debug));
        Assert.False(switches.IsEnabled(LogCategory.Http, LogEventLevel.Debug));
    }

    [Fact]
    public void SetMinimumLevel_None_SuppressesEvenCriticalEvents()
    {
        var switches = new CategoryLevelSwitches();

        switches.SetMinimumLevel(LogCategory.System, LogLevel.None);

        Assert.False(switches.IsEnabled(LogCategory.System, LogEventLevel.Fatal));
    }
}
