using Hekutenantcoreapp.Domain.Common;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Catalogs;
using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;

namespace Hekutenantcoreapp.Infrastructure.Data;

public class HekutenantcoreappDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<DeletedAccount> DeletedAccounts => Set<DeletedAccount>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<UserTenantRole> UserTenantRoles => Set<UserTenantRole>();
    public DbSet<Employee> Employees => Set<Employee>();

    // Global platform config — singleton row, see DatabaseKeepAliveSettings.
    public DbSet<DatabaseKeepAliveSettings> DatabaseKeepAliveSettings => Set<DatabaseKeepAliveSettings>();

    // Global platform config — singleton row, see MultiTenantSettings.
    public DbSet<MultiTenantSettings> MultiTenantSettings => Set<MultiTenantSettings>();

    // Generic translations for free-text field values on any entity — see LocalizedText.
    public DbSet<LocalizedText> LocalizedTexts => Set<LocalizedText>();

    // Geographic reference data: Countries States Cities Database
    // https://github.com/dr5hn/countries-states-cities-database | ODbL v1.0
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();

    // Reportable enum lookup tables — see ReportableEnumCatalog. Rows are seeded
    // automatically (EnumLookupSeeder); never inserted/edited by application code.
    public DbSet<EnumLookup<Gender>> Genders => Set<EnumLookup<Gender>>();
    public DbSet<EnumLookup<DocumentType>> DocumentTypeLookups => Set<EnumLookup<DocumentType>>();
    public DbSet<EnumLookup<TenantMembershipStatus>> TenantMembershipStatusLookups => Set<EnumLookup<TenantMembershipStatus>>();
    public DbSet<EnumLookup<TenantType>> TenantTypeLookups => Set<EnumLookup<TenantType>>();

    public HekutenantcoreappDbContext(DbContextOptions<HekutenantcoreappDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Resolves to 0 (an impossible id) whenever there's no HttpContext or no tenant_id claim,
    // so the ITenantScoped query filter below matches zero rows rather than leaking data.
    // internal (not private) so same-assembly repositories can resolve "my own current tenant".
    internal int CurrentTenantId =>
        int.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue("tenant_id"), out var id) ? id : 0;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Per-entity configuration lives in Data/Configurations/{Entity}Configuration.cs, one
        // file per entity (mirrors this codebase's "one enum per file, one entity per file"
        // convention, which OnModelCreating itself used to be the one holdout from) —
        // auto-discovered here, so a newly-added entity's configuration class needs no
        // separate registration anywhere.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HekutenantcoreappDbContext).Assembly);

        // Reportable enum lookup tables — see ReportableEnumCatalog. Table name and key are
        // configured generically here; the FK from a specific entity's column into one of
        // these tables is still configured explicitly in that entity's own Configurations/
        // file, same as any other foreign key.
        foreach (var (tableName, enumType) in ReportableEnumCatalog.Enums)
        {
            var lookupType = typeof(EnumLookup<>).MakeGenericType(enumType);
            modelBuilder.Entity(lookupType, entity =>
            {
                entity.ToTable(tableName);
                entity.HasKey("Code");
                entity.Property("Code").HasConversion(typeof(string));
            });
        }

        // Every AuditableEntity gets its CreatedAt/UpdatedAt columns indexed, so
        // date-range queries stay fast as these tables grow — applies automatically
        // to future auditable entities too, no per-entity wiring needed.
        // entityType.BaseType != null means this is a TPH subtype sharing its root's
        // table — skip it, the index already exists on the root.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType)) continue;
            if (entityType.BaseType != null) continue;

            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(AuditableEntity.CreatedAt));
            modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(AuditableEntity.UpdatedAt));
        }

        // Every ITenantScoped entity is invisible outside its own tenant by default, and gets
        // a leading TenantId index — applies automatically to future ITenantScoped entities too.
        // The only sanctioned bypass is an explicit .IgnoreQueryFilters() at a handful of named
        // call sites (resolving a user's own tenant memberships/roles, and bootstrap seeding).
        // Skip TPH subtypes (entityType.BaseType != null) — EF only allows a query filter on the
        // root entity type of a hierarchy; the root's filter already applies to subtype queries.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType)) continue;
            if (entityType.BaseType != null) continue;

            if (!entityType.GetIndexes().Any(i => i.Properties.FirstOrDefault()?.Name == nameof(ITenantScoped.TenantId)))
                modelBuilder.Entity(entityType.ClrType).HasIndex(nameof(ITenantScoped.TenantId));

            var method = typeof(HekutenantcoreappDbContext)
                .GetMethod(nameof(BuildTenantFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)method.Invoke(null, new object[] { this })!);
        }

        // Npgsql maps DateTime -> "timestamp with time zone" by default, which requires
        // DateTimeKind.Utc on every value written to (and compared against) that column — but
        // this app's client-facing business-date fields (encounter/test dates, birthdays, etc.)
        // travel the wire as naive "yyyy-MM-dd[THH:mm]" strings with no timezone designator, so
        // they deserialize as Kind=Unspecified, matching how they were always stored under the
        // old SQL Server "datetime" column (see shared/datetime-split.ts on the frontend). Map
        // those specific properties to "timestamp without time zone" instead, which is fine with
        // Kind=Unspecified. Deliberately NOT a blanket "every DateTime" conversion — CreatedAt/
        // UpdatedAt and UserTenantRole.StartsAt/ExpiresAt/RevokedAt are genuine UTC instants
        // (server-stamped via DateTime.UtcNow and compared against it in the login/role-check
        // query), and DatabaseKeepAliveSettings.LastPingAt is likewise server-stamped — all four
        // must stay "timestamp with time zone" or those comparisons/writes start throwing the
        // same Kind mismatch the other way around.
        var naiveDateTimeProperties = new (Type EntityType, string PropertyName)[]
        {
            (typeof(Person), nameof(Person.Birthday)),
            (typeof(Employee), nameof(Employee.HireDate)),
        };

        foreach (var (entityClrType, propertyName) in naiveDateTimeProperties)
        {
            modelBuilder.Entity(entityClrType).Property(propertyName).HasColumnType("timestamp without time zone");
        }
    }

    private static LambdaExpression BuildTenantFilter<TEntity>(HekutenantcoreappDbContext ctx) where TEntity : class, ITenantScoped
    {
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == ctx.CurrentTenantId;
        return filter;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        // Auto-stamp TenantId on insert, mirroring the CreatedBy stamp above — repositories
        // never set it manually. A new row with no resolvable tenant context is refused rather
        // than silently saved without one.
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State != EntityState.Added || entry.Entity.TenantId != 0) continue;

            if (CurrentTenantId == 0)
                throw new InvalidOperationException(
                    $"Cannot save a new {entry.Entity.GetType().Name} without a resolvable tenant context. " +
                    "Set TenantId explicitly for background/seed operations, or ensure the request carries a valid tenant_id claim.");

            entry.Entity.TenantId = CurrentTenantId;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
