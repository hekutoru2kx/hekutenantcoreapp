namespace Hekutenantcoreapp.Domain.Models;

public class UpsertTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? TenantType { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
    public string? Phone { get; set; }
    public string? UrlSite { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public int? AttachmentRetentionDays { get; set; }
}
