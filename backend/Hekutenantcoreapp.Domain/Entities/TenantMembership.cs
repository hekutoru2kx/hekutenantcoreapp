using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Entities;

[Table("tenant_memberships")]
public class TenantMembership : AuditableEntity, ITenantScoped
{
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("status")]
    public TenantMembershipStatus Status { get; set; } = TenantMembershipStatus.Invited;

    public Tenant? Tenant { get; set; }
}
