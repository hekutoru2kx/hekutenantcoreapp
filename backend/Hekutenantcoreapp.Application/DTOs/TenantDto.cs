namespace Hekutenantcoreapp.Application.DTOs;

public class TenantDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TenantType { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
    public string? CountryName { get; set; }
    public string? StateName { get; set; }
    public string? CityName { get; set; }
    public string? Phone { get; set; }
    public string? UrlSite { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public int? AttachmentRetentionDays { get; set; }
}
