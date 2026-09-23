namespace Hekutenantcoreapp.Application.DTOs;

public class UpdateLoggingSettingsDto
{
    public List<UpdateLoggingCategorySettingDto> Categories { get; set; } = new();
    public int RetentionDays { get; set; }
}

public class UpdateLoggingCategorySettingDto
{
    public string Category { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = string.Empty;
}
