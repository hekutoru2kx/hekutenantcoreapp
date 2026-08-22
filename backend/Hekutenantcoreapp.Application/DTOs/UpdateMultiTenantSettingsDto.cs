namespace Hekutenantcoreapp.Application.DTOs;

public class UpdateMultiTenantSettingsDto
{
    public bool DefaultTenantLoginEnabled { get; set; }
    public bool MultiTenantDisabled { get; set; }
    public int? DefaultTenantId { get; set; }
}
