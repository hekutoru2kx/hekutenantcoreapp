namespace Hekutenantcoreapp.Application.DTOs;

public class CountryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Iso2 { get; set; } = string.Empty;
    public string? PhoneCode { get; set; }
}

