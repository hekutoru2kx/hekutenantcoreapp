using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class TenantRoleService : ITenantRoleService
{
    private readonly ITenantRoleRepository _repository;

    public TenantRoleService(ITenantRoleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IList<string>> GetAssignableRoleNamesAsync() =>
        await _repository.GetAssignableRoleNamesAsync();

    public async Task<IList<TenantRoleResult>> GetVisibleRolesAsync() =>
        await _repository.GetVisibleRolesAsync();
}
