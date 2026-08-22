using Hekutenantcoreapp.Domain.Enums.Permissions;

namespace Hekutenantcoreapp.Domain.Catalogs;

// Base tenant-assignable roles, restorable from the System > Roles page. Every module
// should add its permission module here as it's built, so a tenant rarely needs to ask a
// SuperAdmin for a custom role. UserManagementPermission is deliberately never granted
// through this catalog — that module is System-area/SuperAdmin-exclusive (global user
// administration), not a tenant-operational capability like the others.
public static class DefaultRoleCatalog
{
    public static readonly (string RoleName, string Module, string[] Actions)[] Roles =
    [
        ("UserManagementRole", nameof(PersonsPermission), AllActions<PersonsPermission>()),
        ("PersonManagementRole", nameof(PersonsPermission), AllActions<PersonsPermission>()),
        ("EmployeeManagementRole", nameof(EmployeesPermission), AllActions<EmployeesPermission>()),
    ];

    private static string[] AllActions<TEnum>() where TEnum : struct, Enum => Enum.GetNames<TEnum>();
}
