namespace Hekutenantcoreapp.Domain.Models;

public class LoggingSettingsResult
{
    public List<LoggingCategorySettingResult> Categories { get; set; } = new();
    public int RetentionDays { get; set; }
}

public class LoggingCategorySettingResult
{
    public string Category { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = string.Empty;
}
