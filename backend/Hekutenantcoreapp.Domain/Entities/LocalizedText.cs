using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// Generic, polymorphic side-table for translating free-text field values on any entity —
// the data-value counterpart to EnumLookup<TEnum>, which handles fixed enum values.
// EntityName/EntityId is a deliberate polymorphic reference (any future entity), so no FK
// constraint is possible here. Global, not ITenantScoped — a translation of a global catalog
// value is global too. Never holds a row for the base/English value — that stays in the
// entity's own column; a missing row here just means "no override for this language yet."
[Table("localized_texts")]
public class LocalizedText : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("entity_name")]
    public string EntityName { get; set; } = string.Empty;

    [Column("entity_id")]
    public int EntityId { get; set; }

    [Column("field_name")]
    public string FieldName { get; set; } = string.Empty;

    [Column("language_code")]
    public string LanguageCode { get; set; } = string.Empty;

    [Column("value")]
    public string Value { get; set; } = string.Empty;
}
