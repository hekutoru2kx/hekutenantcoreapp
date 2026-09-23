using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Entities;

// One row per LogCategory (six total, seeded once by LoggingSettingsSeeder, never created/deleted
// through the API — only ever six rows exist) holding the live SuperAdmin-set minimum severity for
// that category. Global platform config, not ITenantScoped. LoggingSettingsRefreshHostedService
// polls these and pushes them into the running CategoryLevelSwitches, so a change here takes
// effect without a restart.
[Table("logging_category_settings")]
public class LoggingCategorySettings : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("category")]
    public LogCategory Category { get; set; }

    [Column("minimum_level")]
    public LogLevel MinimumLevel { get; set; }
}
