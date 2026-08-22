namespace Hekutenantcoreapp.Application.DTOs;

public class UpdateProfileDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string PreferredTheme { get; set; } = string.Empty;
    public int? DefaultTenantId { get; set; }
}