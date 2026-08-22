namespace Hekutenantcoreapp.Domain.Models;

public class RoleAssignmentRequest
{
    public string RoleName { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
