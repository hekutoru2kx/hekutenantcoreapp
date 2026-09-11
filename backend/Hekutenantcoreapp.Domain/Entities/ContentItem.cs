using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// Generic, polymorphic content/attachment item — any entity can carry content by picking an
// OwnerType (see ContentOwnerTypes) without a new table. OwnerType/OwnerId is a deliberate
// polymorphic reference (any future entity), so no FK constraint is possible here — the same
// idiom as LocalizedText, generalized to carry a content payload instead of a translated
// string.
//
// ITenantScoped (unlike LocalizedText, which is deliberately global): every owner type in this
// core (Person, Employee, ...) is itself tenant-scoped, so content attached to one is tenant
// data too. That single interface gets this entity the DbContext's automatic tenant query
// filter, TenantId index, and insert-time stamping for free — no manual tenant handling
// anywhere in ContentService/ContentRepository (ported byte-identical from hekucoreapp).
//
// Slot distinguishes two shapes sharing this one table:
//   - Slot null  -> a member of the owner's ordered content list (DisplayOrder within the
//     owner) — e.g. a course's list of materials. No consumer wired yet in this core.
//   - Slot set   -> the single content item for that named slot on the owner — e.g. Person's
//     "ProfilePicture". Enforced unique per (OwnerType, OwnerId, Slot) in ContentItemConfiguration.
[Table("content_items")]
public class ContentItem : AuditableEntity, ITenantScoped
{
    [Column("id")]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("owner_type")]
    public string OwnerType { get; set; } = string.Empty;

    [Column("owner_id")]
    public int OwnerId { get; set; }

    [Column("slot")]
    public string? Slot { get; set; }

    [Column("kind")]
    public ContentKind Kind { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    // Used when Kind = Text. Holds server-sanitized HTML. No consumer wired yet.
    [Column("body")]
    public string? Body { get; set; }

    // External URL payload — Kind = Link/Video/Document, or Image when it isn't blob-backed.
    [Column("url")]
    public string? Url { get; set; }

    // Blob-backed payload — Kind = File, or Image when uploaded rather than linked.
    [Column("stored_file_id")]
    public int? StoredFileId { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    // Null = not yet public. Consumers with no draft workflow (e.g. Person) just always set
    // this on create.
    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    // Null = active. Set = retired, kept for history.
    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    public StoredFile? StoredFile { get; set; }
}
