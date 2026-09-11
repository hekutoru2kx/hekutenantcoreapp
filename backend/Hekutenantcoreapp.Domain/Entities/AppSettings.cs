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

    // Admin-tunable content/upload limits, read by ContentService on every upload — kept here
    // (not appsettings.json) so an admin can change them without a redeploy. appsettings.json's
    // ContentStorage:HardMaxBytes is the absolute ceiling these can't exceed.
    [Column("content_max_bytes")]
    public long ContentMaxBytes { get; set; } = 5 * 1024 * 1024;

    [Column("content_allowed_content_types")]
    public string ContentAllowedContentTypes { get; set; } = "image/jpeg,image/png,image/webp";

    [Column("content_max_image_dimension")]
    public int ContentMaxImageDimension { get; set; } = 2048;

    [Column("content_avatar_max_dimension")]
    public int ContentAvatarMaxDimension { get; set; } = 512;
}
