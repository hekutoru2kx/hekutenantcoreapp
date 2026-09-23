namespace Hekutenantcoreapp.Domain.Models;

public class SystemLogEntryResult
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

public class SystemLogsPageResult
{
    public List<SystemLogEntryResult> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
