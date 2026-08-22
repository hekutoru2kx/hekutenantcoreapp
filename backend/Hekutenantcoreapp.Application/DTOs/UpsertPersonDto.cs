namespace Hekutenantcoreapp.Application.DTOs;

public class UpsertPersonDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    public string? DocumentType { get; set; }
    public string? DocumentId { get; set; }
    public string? Phone { get; set; }
    public string? PhoneExtension { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? Gender { get; set; }
    public string? AlternativePhone { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? CityId { get; set; }
}