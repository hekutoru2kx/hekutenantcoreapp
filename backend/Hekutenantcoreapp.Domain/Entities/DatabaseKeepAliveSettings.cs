using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Entities;

// A singleton row (Id is always 1 — seeded once at startup by DatabaseKeepAliveSettingsSeeder,
// never created/deleted through the API) configuring the optional background ping that keeps
// the serverless Azure SQL database from auto-pausing during a chosen active window. Global
// platform config, not ITenantScoped — same tier as Tenant itself.
[Table("database_keep_alive_settings")]
public class DatabaseKeepAliveSettings : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("interval_amount")]
    public int IntervalAmount { get; set; } = 45;

    [Column("interval_unit")]
    public KeepAliveIntervalUnit IntervalUnit { get; set; } = KeepAliveIntervalUnit.Minutes;

    [Column("gmt_offset_hours")]
    public int GmtOffsetHours { get; set; }

    [Column("active_start_time")]
    public TimeSpan ActiveStartTime { get; set; } = TimeSpan.Zero;

    [Column("active_end_time")]
    public TimeSpan ActiveEndTime { get; set; } = new TimeSpan(23, 59, 0);

    // Written only by DatabaseKeepAliveHostedService — not user-editable via the API.
    [Column("last_ping_at")]
    public DateTime? LastPingAt { get; set; }
}
