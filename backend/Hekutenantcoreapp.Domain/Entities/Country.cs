using System.ComponentModel.DataAnnotations.Schema;

// Data source: Countries States Cities Database
// https://github.com/dr5hn/countries-states-cities-database | ODbL v1.0
namespace Hekutenantcoreapp.Domain.Entities;

[Table("countries")]
public class Country
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("iso2")]
    public string Iso2 { get; set; } = string.Empty;

    [Column("iso3")]
    public string Iso3 { get; set; } = string.Empty;

    [Column("phone_code")]
    public string? PhoneCode { get; set; }

    [Column("capital")]
    public string? Capital { get; set; }

    [Column("currency")]
    public string? Currency { get; set; }

    [Column("region")]
    public string? Region { get; set; }

    [Column("subregion")]
    public string? Subregion { get; set; }
}