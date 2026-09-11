using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IContentRepository
{
    Task<ContentItem?> GetByIdAsync(int id);

    Task<ContentItem?> GetSlotAsync(string ownerType, int ownerId, string slot);

    Task<IReadOnlyList<ContentItem>> GetForOwnerAsync(string ownerType, int ownerId, bool includeUnpublished, bool includeArchived);

    // Creates the item for (ownerType, ownerId, slot) if none exists, or replaces its file if
    // one does. Returns the saved item and the StoredFile it replaced (null on first upload) —
    // the caller (ContentService) is responsible for deleting the replaced blob.
    Task<(ContentItem Item, StoredFile? ReplacedFile)> UpsertFileSlotAsync(string ownerType, int ownerId, string slot, StoredFile file, ContentKind kind, string title);

    // Removes the item and its StoredFile row, returning the removed file (null if the slot was
    // empty) so the caller can delete its blob.
    Task<StoredFile?> DeleteSlotAsync(string ownerType, int ownerId, string slot);
}
