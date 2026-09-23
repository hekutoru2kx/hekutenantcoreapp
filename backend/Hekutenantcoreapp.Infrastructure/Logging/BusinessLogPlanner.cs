using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// Pure planning step for HekutenantcoreappDbContext's business-CRUD logging: given a snapshot of
// what changed in one SaveChangesAsync call, decides what log lines to emit. Kept separate from
// SaveChangesAsync itself so it's unit-testable without a real save. IAggregateItem entries never
// get their own line — they collapse into one "{Root} {Id} items updated" line per distinct root
// touched in the save, regardless of how many items changed or whether they were added/modified/
// removed (add-then-remove-and-recreate is common enough for item edits that distinguishing the
// three isn't worth it — see the "items updated" phrasing decision).
public static class BusinessLogPlanner
{
    public sealed record EntrySnapshot(EntityEntry Entry, EntityState State, string EntityTypeName, IAggregateItem? AggregateItem);

    public static IReadOnlyList<string> Plan(IEnumerable<EntrySnapshot> snapshots)
    {
        var lines = new List<string>();
        var itemGroups = new HashSet<(string RootName, object RootId)>();

        foreach (var snapshot in snapshots)
        {
            if (snapshot.AggregateItem is { } item)
            {
                itemGroups.Add((item.AggregateRootName, item.AggregateRootId));
                continue;
            }

            var resourceKey = snapshot.State switch
            {
                EntityState.Added => "EntityCreated",
                EntityState.Modified => "EntityUpdated",
                EntityState.Deleted => "EntityDeleted",
                _ => null
            };
            if (resourceKey is null) continue;

            lines.Add(LogText.Get(resourceKey, snapshot.EntityTypeName, GetPrimaryKeyValue(snapshot.Entry) ?? "?"));
        }

        foreach (var (rootName, rootId) in itemGroups)
            lines.Add(LogText.Get("AggregateItemsUpdated", rootName, rootId));

        return lines;
    }

    private static object? GetPrimaryKeyValue(EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        return pk is { Properties.Count: > 0 } ? entry.Property(pk.Properties[0].Name).CurrentValue : null;
    }
}
