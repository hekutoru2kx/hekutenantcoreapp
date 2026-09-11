namespace Hekutenantcoreapp.Domain.Constants;

// ContentItem.OwnerType is a deliberate polymorphic string reference (see ContentItem) — no FK
// is possible, so this is the single allow-list every write path checks against instead of
// accepting an arbitrary string at the call site. Add a new owner type here when a new entity
// opts into the content capability; that's the entire schema-side cost of adding one.
public static class ContentOwnerTypes
{
    public const string Person = "Person";

    public static readonly string[] All = [Person];

    public static bool IsValid(string ownerType) => All.Contains(ownerType);
}
