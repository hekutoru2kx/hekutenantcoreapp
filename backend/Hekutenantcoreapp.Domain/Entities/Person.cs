using Hekutenantcoreapp.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hekutenantcoreapp.Domain.Enums;

namespace Hekutenantcoreapp.Domain.Entities;

[Table("persons")]
public class Person : AuditableEntity, ITenantScoped
{
    [Column("id")]
    public int Id { get; set; }

    [Column("tenant_id")]
    public int TenantId { get; set; }

    [Column("user_id")]
    public string? UserId { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("birthday")]
    public DateTime? Birthday { get; set; }

    [Column("document_type")]
    public DocumentType? DocumentType { get; set; }

    [Column("document_id")]
    public string? DocumentId { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("phone_extension")]
    public string? PhoneExtension { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("gender")]
    public Gender? Gender { get; set; }

    [Column("alternative_phone")]
    public string? AlternativePhone { get; set; }

    [Column("country_id")]
    public int? CountryId { get; set; }

    [Column("state_id")]
    public int? StateId { get; set; }

    [Column("city_id")]
    public int? CityId { get; set; }

    [Column("postal_code")]
    public string? PostalCode { get; set; }

    public Country? Country { get; set; }
    public State? State { get; set; }
    public City? City { get; set; }
    public Tenant? Tenant { get; set; }
}