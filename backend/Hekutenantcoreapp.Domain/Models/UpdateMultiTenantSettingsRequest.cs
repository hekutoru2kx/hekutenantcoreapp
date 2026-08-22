namespace Hekutenantcoreapp.Domain.Models;

public class UpdateMultiTenantSettingsRequest
{
    public bool DefaultTenantLoginEnabled { get; set; }
    public bool MultiTenantDisabled { get; set; }
    public int? DefaultTenantId { get; set; }
}
