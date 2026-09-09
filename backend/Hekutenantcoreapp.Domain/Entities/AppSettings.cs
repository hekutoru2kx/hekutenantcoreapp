using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// A singleton row (Id is always 1 — seeded once at startup by AppSettingsSeeder, never
// created/deleted through the API). Holds app-wide, admin-configurable settings.
[Table("app_settings")]
public class AppSettings : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    // When true, a newly registered user must confirm their email address (via a link sent
    // on registration) before they can log in. When false, accounts are created already
    // confirmed and there is no gate. AuthService reads this at registration and login.
    [Column("require_email_confirmation")]
    public bool RequireEmailConfirmation { get; set; }
}
