using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Catalogs;

// Enums that back a real, persisted entity column (as opposed to permission/claim enums
// like PersonsPermission, which are never stored in a column) get a lookup table, so their
// full domain of values is queryable/joinable directly from the database rather than only
// inferable from application code. Register a newly-persisted enum here and give it a table
// in HekutenantcoreappDbContext — nothing else to remember; EnumLookupSeeder keeps the rows in sync
// with the enum's own definition automatically.
public static class ReportableEnumCatalog
{
    public static readonly (string TableName, Type EnumType)[] Enums =
    [
        ("genders", typeof(Gender)),
        ("document_types", typeof(DocumentType)),
        ("tenant_membership_statuses", typeof(TenantMembershipStatus)),
        ("tenant_types", typeof(TenantType)),
    ];
}
