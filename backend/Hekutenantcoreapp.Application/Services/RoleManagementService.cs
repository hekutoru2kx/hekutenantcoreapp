using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Catalogs;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class RoleManagementService : IRoleManagementService
{
    private readonly IRoleManagementRepository _repository;

    public RoleManagementService(IRoleManagementRepository repository)
    {
        _repository = repository;
    }

    public async Task<IList<RoleResult>> GetRolesAsync() =>
        await _repository.GetRolesAsync();

    public async Task CreateRoleAsync(string name) =>
        await _repository.CreateRoleAsync(name);

    public async Task DeleteRoleAsync(string name) =>
        await _repository.DeleteRoleAsync(name);

    public async Task AssignClaimsAsync(string roleName, IList<PermissionClaimResult> claims) =>
        await _repository.AssignClaimsAsync(roleName, claims);

    public IList<PermissionModuleResult> GetPermissionCatalog() =>
        PermissionCatalog.Modules
            .Select(m => new PermissionModuleResult
            {
                Module = m.Module,
                Actions = Enum.GetNames(m.EnumType)
            })
            .ToList();

    public async Task RestoreDefaultRolesAsync() =>
        await _repository.RestoreDefaultRolesAsync();
}
