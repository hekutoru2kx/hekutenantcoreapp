namespace Hekutenantcoreapp.Domain.Common;

// Marks an entity as a child of some other aggregate root, purely for HekutenantcoreappDbContext's
// business-CRUD logging (see BusinessLogPlanner): a changed IAggregateItem never gets its own log
// line, it folds into one "{Root} {Id} items updated" line per root per save. Explicit interface
// implementation on purpose — these two members exist only for that log-grouping code to read,
// not as part of the entity's normal public surface (and so EF's convention-based model discovery
// never mistakes them for a mapped column).
//
// Ported from gestamind (DiagnosticTestItem/Observation/HistoryFieldOption there). No entity in
// this repo's own 11-entity AuditableEntity list is a genuine single-hop item-of-a-root the same
// way — none currently implements this interface. It stays here as pure infrastructure for the
// next entity that does need it.
public interface IAggregateItem
{
    string AggregateRootName { get; }
    object AggregateRootId { get; }
}
