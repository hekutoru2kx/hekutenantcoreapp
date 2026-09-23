using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hekutenantcoreapp.Tests.Infrastructure.Logging;

public class BusinessLogPlannerTests
{
    private static HekutenantcoreappDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HekutenantcoreappDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new HekutenantcoreappDbContext(options, new HttpContextAccessor(), NullCategoryLogger.Instance);
    }

    private static BusinessLogPlanner.EntrySnapshot Snapshot(HekutenantcoreappDbContext context, object entity)
    {
        var entry = context.Entry(entity);
        return new BusinessLogPlanner.EntrySnapshot(entry, entry.State, entity.GetType().Name, entity as IAggregateItem);
    }

    // No entity in this repo implements IAggregateItem yet (see that interface's own doc comment
    // — none of the 11 AuditableEntity-derived entities here is a genuine single-hop item-of-a-
    // root the way gestamind's DiagnosticTestItem/Observation/HistoryFieldOption are). This test
    // double exercises the roll-up path with the same shape a real implementer would have.
    // BusinessLogPlanner never reads EntrySnapshot.Entry for an item — only its AggregateItem — so
    // pairing this with an unrelated tracked entity for the Entry field is safe and avoids needing
    // a real mapped IAggregateItem entity just for this test.
    private sealed class FakeAggregateItem : IAggregateItem
    {
        public FakeAggregateItem(string rootName, object rootId)
        {
            AggregateRootName = rootName;
            AggregateRootId = rootId;
        }

        public string AggregateRootName { get; }
        public object AggregateRootId { get; }
    }

    private static BusinessLogPlanner.EntrySnapshot ItemSnapshot(HekutenantcoreappDbContext context, int trackingId, string rootName, object rootId)
    {
        var placeholder = new Tenant { Id = trackingId, Name = $"placeholder-{trackingId}", TenantType = TenantType.Standard, IsActive = true };
        context.Attach(placeholder);
        var entry = context.Entry(placeholder);
        return new BusinessLogPlanner.EntrySnapshot(entry, entry.State, "Item", new FakeAggregateItem(rootName, rootId));
    }

    [Fact]
    public void Plan_AddedRootEntity_ProducesCreatedLine()
    {
        using var context = CreateContext(nameof(Plan_AddedRootEntity_ProducesCreatedLine));
        var tenant = new Tenant { Id = 7, Name = "Acme", TenantType = TenantType.Standard, IsActive = true };
        context.Add(tenant);

        var lines = BusinessLogPlanner.Plan([Snapshot(context, tenant)]);

        Assert.Single(lines);
        Assert.Equal("Tenant 7 created", lines[0]);
    }

    [Fact]
    public void Plan_ModifiedRootEntity_ProducesUpdatedLine()
    {
        using var context = CreateContext(nameof(Plan_ModifiedRootEntity_ProducesUpdatedLine));
        var tenant = new Tenant { Id = 12, Name = "Acme", TenantType = TenantType.Standard, IsActive = true };
        context.Attach(tenant);
        context.Entry(tenant).State = EntityState.Modified;

        var lines = BusinessLogPlanner.Plan([Snapshot(context, tenant)]);

        Assert.Single(lines);
        Assert.Equal("Tenant 12 updated", lines[0]);
    }

    [Fact]
    public void Plan_DeletedRootEntity_ProducesDeletedLine()
    {
        using var context = CreateContext(nameof(Plan_DeletedRootEntity_ProducesDeletedLine));
        var tenant = new Tenant { Id = 3, Name = "Acme", TenantType = TenantType.Standard, IsActive = true };
        context.Attach(tenant);
        context.Entry(tenant).State = EntityState.Deleted;

        var lines = BusinessLogPlanner.Plan([Snapshot(context, tenant)]);

        Assert.Single(lines);
        Assert.Equal("Tenant 3 deleted", lines[0]);
    }

    [Fact]
    public void Plan_MultipleItemsUnderSameRoot_ProducesOneRollUpLine()
    {
        using var context = CreateContext(nameof(Plan_MultipleItemsUnderSameRoot_ProducesOneRollUpLine));
        var item1 = ItemSnapshot(context, 1, "Tenant", 7);
        var item2 = ItemSnapshot(context, 2, "Tenant", 7);

        var lines = BusinessLogPlanner.Plan([item1, item2]);

        Assert.Single(lines);
        Assert.Equal("Tenant 7 items updated", lines[0]);
    }

    [Fact]
    public void Plan_ItemsUnderDifferentRoots_ProducesOneLinePerRoot()
    {
        using var context = CreateContext(nameof(Plan_ItemsUnderDifferentRoots_ProducesOneLinePerRoot));
        var item1 = ItemSnapshot(context, 1, "Tenant", 7);
        var item2 = ItemSnapshot(context, 2, "Tenant", 9);

        var lines = BusinessLogPlanner.Plan([item1, item2]);

        Assert.Equal(2, lines.Count);
        Assert.Contains("Tenant 7 items updated", lines);
        Assert.Contains("Tenant 9 items updated", lines);
    }

    [Fact]
    public void Plan_MixOfRootAndItemChanges_ProducesBothKindsOfLines()
    {
        using var context = CreateContext(nameof(Plan_MixOfRootAndItemChanges_ProducesBothKindsOfLines));
        var tenant = new Tenant { Id = 5, Name = "Acme", TenantType = TenantType.Standard, IsActive = true };
        context.Add(tenant);
        var item = ItemSnapshot(context, 1, "Tenant", 5);

        var lines = BusinessLogPlanner.Plan([Snapshot(context, tenant), item]);

        Assert.Equal(2, lines.Count);
        Assert.Contains("Tenant 5 created", lines);
        Assert.Contains("Tenant 5 items updated", lines);
    }

    [Fact]
    public void Plan_NoChanges_ProducesNoLines()
    {
        var lines = BusinessLogPlanner.Plan([]);

        Assert.Empty(lines);
    }
}
