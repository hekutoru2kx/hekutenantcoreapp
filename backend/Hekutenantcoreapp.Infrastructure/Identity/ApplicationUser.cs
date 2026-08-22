using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    [Column("preferred_language")]
    public string PreferredLanguage { get; set; } = "en";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; } = false;

    [Column("preferred_theme")]
    public string PreferredTheme { get; set; } = "azure";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The tenant this user logs straight into when MultiTenantSettings.DefaultTenantLoginEnabled
    // (or MultiTenantDisabled) is on and this points at one of their active memberships. Self-set
    // via the Profile page, or set/overridden by an admin — see UserManagementController.
    [Column("default_tenant_id")]
    public int? DefaultTenantId { get; set; }
}