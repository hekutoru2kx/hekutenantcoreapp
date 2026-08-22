using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// A singleton row (Id is always 1 — seeded once at startup by MultiTenantSettingsSeeder, never
// created/deleted through the API), same tier as DatabaseKeepAliveSettings. Drives the tenant
// resolution AuthService.ResolveAndMintAsync performs at login/registration.
[Table("multi_tenant_settings")]
public class MultiTenantSettings : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    // When true, a user whose personal DefaultTenantId (or, failing that, this row's
    // DefaultTenantId) matches one of their memberships skips the post-login tenant-picker.
    [Column("default_tenant_login_enabled")]
    public bool DefaultTenantLoginEnabled { get; set; }

    // When true, tenant-selection UI (registration dropdown, nav-bar "join another tenant")
    // is hidden app-wide and DefaultTenantLoginEnabled's resolution behavior is forced on —
    // computed at resolution time, never persisted onto DefaultTenantLoginEnabled itself.
    [Column("multi_tenant_disabled")]
    public bool MultiTenantDisabled { get; set; }

    // The app-wide fallback tenant. Required (validated in MultiTenantSettingsService) before
    // either flag above can be enabled.
    [Column("default_tenant_id")]
    public int? DefaultTenantId { get; set; }

    public Tenant? DefaultTenant { get; set; }
}
