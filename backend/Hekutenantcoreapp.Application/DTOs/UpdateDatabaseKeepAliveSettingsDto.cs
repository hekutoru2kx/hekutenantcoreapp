namespace Hekutenantcoreapp.Application.DTOs;

public class UpdateDatabaseKeepAliveSettingsDto
{
    public bool IsEnabled { get; set; }
    public int IntervalAmount { get; set; }
    public string IntervalUnit { get; set; } = string.Empty;
    public int GmtOffsetHours { get; set; }
    public string ActiveStartTime { get; set; } = string.Empty;
    public string ActiveEndTime { get; set; } = string.Empty;
}
