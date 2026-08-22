namespace Hekutenantcoreapp.Domain.Models;

public class EmployeeListQuery : PagedQuery
{
    public string? SortBy { get; set; } = "JobTitle";
}
