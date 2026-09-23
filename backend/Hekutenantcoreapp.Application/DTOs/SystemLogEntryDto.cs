namespace Hekutenantcoreapp.Application.DTOs;

public class SystemLogEntryDto
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public int? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? TraceId { get; set; }
}

public class SystemLogsPageDto
{
    public List<SystemLogEntryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
