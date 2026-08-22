using System.ComponentModel.DataAnnotations.Schema;

// Data source: Countries States Cities Database
// https://github.com/dr5hn/countries-states-cities-database | ODbL v1.0
namespace Hekutenantcoreapp.Domain.Entities;

[Table("states")]
public class State
{
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("state_code")]
    public string? StateCode { get; set; }

    [Column("country_id")]
    public int CountryId { get; set; }

    public Country? Country { get; set; }
}