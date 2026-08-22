namespace Hekutenantcoreapp.Application.DTOs;

public class GoogleAuthDto
{
    public string IdToken { get; set; } = string.Empty;
    public int? TenantId { get; set; }
}
