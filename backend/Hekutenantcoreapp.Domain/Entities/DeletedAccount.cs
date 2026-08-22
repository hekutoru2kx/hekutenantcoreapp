using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hekutenantcoreapp.Domain.Entities;

[Table("deleted_accounts")]
public class DeletedAccount : AuditableEntity
{
    [Column("id")]
    public int Id { get; set; }

    [Column("normalized_email")]
    public string NormalizedEmail { get; set; } = string.Empty;
}
