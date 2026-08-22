namespace Hekutenantcoreapp.Domain.Models;

public class UserProfileResult
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = string.Empty;
    public string PreferredTheme { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int? DefaultTenantId { get; set; }
}