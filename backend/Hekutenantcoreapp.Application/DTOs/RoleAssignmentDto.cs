namespace Hekutenantcoreapp.Application.DTOs;

public class RoleAssignmentDto
{
    public string RoleName { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsPending { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedByName { get; set; } = string.Empty;
}
