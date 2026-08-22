namespace Hekutenantcoreapp.Domain.Models;

public class MultiTenantSettingsResult
{
    public bool DefaultTenantLoginEnabled { get; set; }
    public bool MultiTenantDisabled { get; set; }
    public int? DefaultTenantId { get; set; }
    public string? DefaultTenantName { get; set; }
}
