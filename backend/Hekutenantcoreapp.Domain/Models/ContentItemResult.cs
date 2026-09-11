namespace Hekutenantcoreapp.Domain.Models;

public class ContentItemResult
{
    public int Id { get; set; }
    public string OwnerType { get; set; } = string.Empty;
    public int OwnerId { get; set; }
    public string? Slot { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Url { get; set; }
    public bool HasFile { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }
}
