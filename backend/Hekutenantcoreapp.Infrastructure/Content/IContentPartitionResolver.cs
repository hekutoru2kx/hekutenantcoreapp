namespace Hekutenantcoreapp.Infrastructure.Content;

// Internal to the storage layer — ContentService (Application) never sees a partition; each
// IContentStorage implementation resolves its own via this before building a blob path. See
// TenantContentPartitionResolver for how this core resolves it (the caller's current tenant's
// Tenant.StoragePrefix). hekucoreapp/ludemia's equivalent always returns the constant "global" —
// that's the only DI difference in the blob layer between the single- and multi-tenant variants.
public interface IContentPartitionResolver
{
    Task<string> ResolveAsync(CancellationToken ct = default);
}
