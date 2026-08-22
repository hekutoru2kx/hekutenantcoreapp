namespace Hekutenantcoreapp.Domain.Models;

public class GenerateTokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public int? TenantId { get; set; }
}