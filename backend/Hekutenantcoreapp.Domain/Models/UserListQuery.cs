namespace Hekutenantcoreapp.Domain.Models;

public class UserListQuery : PagedQuery
{
    public string? SortBy { get; set; } = "UserName";
    public string? RoleFilter { get; set; }
    public bool? StatusFilter { get; set; }
}