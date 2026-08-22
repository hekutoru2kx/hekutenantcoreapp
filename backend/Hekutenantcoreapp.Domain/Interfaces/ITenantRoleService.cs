using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface ITenantRoleService
{
    // Role names assignable within the caller's current tenant: every role except SuperAdmin.
    // Role creation/claim-editing is SuperAdmin-only (System area) — tenant admins only
    // read the shared catalog and assign roles to their employees.
    Task<IList<string>> GetAssignableRoleNamesAsync();

    // Same visibility rule as above, but with claims attached, so a tenant admin can review
    // what a role grants before assigning it.
    Task<IList<TenantRoleResult>> GetVisibleRolesAsync();
}
