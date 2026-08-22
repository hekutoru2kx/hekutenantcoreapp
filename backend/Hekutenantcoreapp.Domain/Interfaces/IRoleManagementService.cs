using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IRoleManagementService
{
    Task<IList<RoleResult>> GetRolesAsync();
    Task CreateRoleAsync(string name);
    Task DeleteRoleAsync(string name);
    Task AssignClaimsAsync(string roleName, IList<PermissionClaimResult> claims);
    IList<PermissionModuleResult> GetPermissionCatalog();
    Task RestoreDefaultRolesAsync();
}
