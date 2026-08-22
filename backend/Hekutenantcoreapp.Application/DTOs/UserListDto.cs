namespace Hekutenantcoreapp.Application.DTOs;

public class UserListDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }

    public DateTime CreatedAt { get; set; }
    public int? DefaultTenantId { get; set; }
}