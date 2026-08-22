namespace Hekutenantcoreapp.Application.DTOs;

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
    public IList<PermissionClaimDto> Claims { get; set; } = new List<PermissionClaimDto>();
}
