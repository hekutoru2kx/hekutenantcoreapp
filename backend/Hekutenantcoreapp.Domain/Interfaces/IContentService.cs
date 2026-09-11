using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

// Generic, owner-agnostic content service — any entity can opt in by picking a value from
// ContentOwnerTypes and calling this from its own controller/endpoints; this interface never
// knows which permission gates a call, only the caller's feature does, and never knows about
// tenancy — ContentItem/StoredFile being ITenantScoped handles that transparently. Ported
// verbatim from hekucoreapp 2026-09-11 (only the namespace changed).
public interface IContentService
{
    Task<ContentItemResult?> GetByIdAsync(int id);

    // "Slot" is the singleton shape (e.g. Person's "ProfilePicture") — at most one item per
    // (ownerType, ownerId, slot).
    Task<ContentItemResult?> GetSlotAsync(string ownerType, int ownerId, string slot);

    // "Collection" shape (Slot = null) — no consumer wired yet in this core.
    Task<IReadOnlyList<ContentItemResult>> GetForOwnerAsync(string ownerType, int ownerId, bool includeUnpublished = false, bool includeArchived = false);

    // Validates size/type/dimensions against AppSettings, resizes+re-encodes images
    // (strips EXIF), stores the blob, and creates or replaces the item in that slot.
    // Replacing hard-deletes the previous blob.
    Task<ContentItemResult> UploadToSlotAsync(string ownerType, int ownerId, string slot, Stream fileStream, string fileName, string? declaredContentType);

    // Hard delete: removes the item, its StoredFile row, and its blob.
    Task DeleteSlotAsync(string ownerType, int ownerId, string slot);

    Task<(Stream Stream, string ContentType, string FileName, string ETag)> OpenFileAsync(int id);
}
