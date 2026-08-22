namespace Hekutenantcoreapp.Domain.Models;

public class PersonListQuery : PagedQuery
{
    public string? SortBy { get; set; } = "LastName";
    public int? CountryId { get; set; }
}