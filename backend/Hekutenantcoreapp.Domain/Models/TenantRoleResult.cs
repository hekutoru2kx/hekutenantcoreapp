namespace Hekutenantcoreapp.Domain.Models;

public class TenantRoleResult
{
    public string Name { get; set; } = string.Empty;
    public IList<PermissionClaimResult> Claims { get; set; } = new List<PermissionClaimResult>();
}
