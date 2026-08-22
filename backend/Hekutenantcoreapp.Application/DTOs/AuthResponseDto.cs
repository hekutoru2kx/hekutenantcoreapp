namespace Hekutenantcoreapp.Application.DTOs;

public class AuthResponseDto
{
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    public bool MustChangePassword { get; set; }

    public string PreferredTheme { get; set; } = "azure";

    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public IList<TenantSummaryDto> AvailableTenants { get; set; } = new List<TenantSummaryDto>();
    public bool MultiTenantDisabled { get; set; }
}