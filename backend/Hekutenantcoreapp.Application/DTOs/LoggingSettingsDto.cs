namespace Hekutenantcoreapp.Application.DTOs;

public class LoggingSettingsDto
{
    public List<LoggingCategorySettingDto> Categories { get; set; } = new();
    public int RetentionDays { get; set; }
}

public class LoggingCategorySettingDto
{
    public string Category { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = string.Empty;
}
