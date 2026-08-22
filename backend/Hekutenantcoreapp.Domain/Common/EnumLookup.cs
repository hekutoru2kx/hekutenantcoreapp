namespace Hekutenantcoreapp.Domain.Common;

// Reference-table row for a persisted enum's full domain of values — see
// ReportableEnumCatalog for which enums get one. One closed generic type per enum
// (EnumLookup<Gender>, EnumLookup<DocumentType>, ...), each mapped to its own table in
// HekutenantcoreappDbContext.OnModelCreating and seeded automatically at startup (EnumLookupSeeder)
// directly from the enum's own definition — never hand-maintained, so it can't drift.
public class EnumLookup<TEnum> where TEnum : struct, Enum
{
    // Typed as the enum itself (converted to its string name for storage — see
    // HekutenantcoreappDbContext) rather than as a plain string, so EF Core's foreign-key type
    // check accepts a reference from an entity's TEnum-typed column: EF compares declared
    // CLR types for FK compatibility, before either side's value converter is applied.
    public TEnum Code { get; set; }
    public int SortOrder { get; set; }
}
