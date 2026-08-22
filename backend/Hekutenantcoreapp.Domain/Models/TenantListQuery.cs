namespace Hekutenantcoreapp.Domain.Models;

public class TenantListQuery : PagedQuery
{
    public string? SortBy { get; set; } = "Name";
}
