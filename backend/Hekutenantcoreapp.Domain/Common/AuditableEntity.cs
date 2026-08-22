using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Common;

public abstract class AuditableEntity
{
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;
}