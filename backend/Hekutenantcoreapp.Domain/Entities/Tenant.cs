using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Entities;

[Table("tenants")]
public class Tenant : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("tenant_type")]
    public TenantType TenantType { get; set; }

    [Column("country_id")]
    public int? CountryId { get; set; }

    [Column("state_id")]
    public int? StateId { get; set; }

    [Column("city_id")]
    public int? CityId { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("url_site")]
    public string? UrlSite { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("attachment_retention_days")]
    public int? AttachmentRetentionDays { get; set; }

    // Opaque per-tenant slug used to partition blob storage (see TenantContentPartitionResolver)
    // — not this tenant's int Id, so blob paths/URLs don't expose ids or let anyone enumerate
    // tenants by counting. Generated once, automatically, in
    // HekutenantcoreappDbContext.SaveChangesAsync whenever a new Tenant is inserted (covers
    // every construction site — admin-created, the startup default tenant, tests — with no
    // per-call-site wiring). Immutable after that.
    [Column("storage_prefix")]
    public string StoragePrefix { get; set; } = string.Empty;

    public Country? Country { get; set; }
    public State? State { get; set; }
    public City? City { get; set; }
}
