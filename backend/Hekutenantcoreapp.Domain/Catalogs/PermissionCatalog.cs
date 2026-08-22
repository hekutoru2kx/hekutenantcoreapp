using Hekutenantcoreapp.Domain.Enums.Permissions;

namespace Hekutenantcoreapp.Domain.Catalogs;

public static class PermissionCatalog
{
    public static readonly (string Module, Type EnumType)[] Modules =
    [
        (nameof(RolesPermission), typeof(RolesPermission)),
        (nameof(UserManagementPermission), typeof(UserManagementPermission)),
        (nameof(PersonsPermission), typeof(PersonsPermission)),
        (nameof(TenantsPermission), typeof(TenantsPermission)),
        (nameof(EmployeesPermission), typeof(EmployeesPermission)),
        (nameof(MultiTenantSettingsPermission), typeof(MultiTenantSettingsPermission)),
    ];
}
