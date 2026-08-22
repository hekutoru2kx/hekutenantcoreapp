namespace Hekutenantcoreapp.Domain.Models;

// Safety valve for "export everything matching this search" endpoints — distinct from
// PagedQuery.MaxPageSize, which caps the normal paged browsing endpoints. Export is triggered by
// an already-authorized admin who could already see every row anyway (just one page at a time),
// so this cap exists only to guard against a truly pathological unfiltered request, not to
// restrict access. Null or <= 0 means unlimited.
public record ExportSettings(int? MaxRows)
{
    public bool IsUnlimited => !MaxRows.HasValue || MaxRows.Value <= 0;
}
