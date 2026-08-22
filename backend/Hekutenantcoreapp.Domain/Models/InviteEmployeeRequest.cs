namespace Hekutenantcoreapp.Domain.Models;

public class InviteEmployeeRequest
{
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime? RoleExpiresAt { get; set; }
    public string? JobTitle { get; set; }
    public DateTime? HireDate { get; set; }
}
