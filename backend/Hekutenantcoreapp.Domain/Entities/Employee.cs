using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

[Table("employees")]
public class Employee : AuditableEntity, ITenantScoped
{
    [Column("id")]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("job_title")]
    public string? JobTitle { get; set; }

    [Column("hire_date")]
    public DateTime? HireDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
}
