namespace Hekutenantcoreapp.Domain.Models;

public class SystemLogsQuery
{
    public int? TenantId { get; set; }
    public string? Category { get; set; }
    public string? Level { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
