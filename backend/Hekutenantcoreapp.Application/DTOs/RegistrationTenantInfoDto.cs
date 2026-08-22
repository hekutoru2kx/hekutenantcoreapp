namespace Hekutenantcoreapp.Application.DTOs;

public class RegistrationTenantInfoDto
{
    public bool TenantSelectionEnabled { get; set; }
    public int? DefaultTenantId { get; set; }
    public string? DefaultTenantName { get; set; }
}
