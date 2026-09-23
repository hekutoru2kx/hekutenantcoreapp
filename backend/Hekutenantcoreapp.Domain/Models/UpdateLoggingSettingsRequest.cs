namespace Hekutenantcoreapp.Domain.Models;

public class UpdateLoggingSettingsRequest
{
    public List<UpdateLoggingCategorySettingRequest> Categories { get; set; } = new();
    public int RetentionDays { get; set; }
}

public class UpdateLoggingCategorySettingRequest
{
    public string Category { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = string.Empty;
}
