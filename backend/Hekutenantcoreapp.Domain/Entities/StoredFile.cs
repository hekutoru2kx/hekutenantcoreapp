using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

// Blob metadata only — the bytes live in blob storage (or App_Data/uploads in LocalDisk dev
// mode), never in Postgres. BlobName already carries the storage partition prefix — the
// owning tenant's Tenant.StoragePrefix (see TenantContentPartitionResolver), so a tenant's
// files can be found/exported/purged by blob-name prefix alone, without a DB join.
//
// ITenantScoped for the same reason as ContentItem: a stray direct query against this table
// (bypassing ContentItem) still can't leak another tenant's files.
[Table("stored_files")]
public class StoredFile : AuditableEntity, ITenantScoped
{
    [Column("id")]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("container")]
    public string Container { get; set; } = string.Empty;

    [Column("blob_name")]
    public string BlobName { get; set; } = string.Empty;

    [Column("original_file_name")]
    public string OriginalFileName { get; set; } = string.Empty;

    [Column("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [Column("byte_size")]
    public long ByteSize { get; set; }

    // Integrity / dedup, and doubles as the download endpoint's ETag.
    [Column("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    // Images only.
    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }
}
