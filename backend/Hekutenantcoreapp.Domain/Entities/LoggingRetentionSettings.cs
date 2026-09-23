using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// A singleton row (Id is always 1 — seeded once at startup, never created/deleted through the
// API) holding how long system_logs rows are kept before a daily sweep deletes them. Global
// platform config, not ITenantScoped — same tier as Tenant itself.
[Table("logging_retention_settings")]
public class LoggingRetentionSettings : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("retention_days")]
    public int RetentionDays { get; set; } = 30;
}
