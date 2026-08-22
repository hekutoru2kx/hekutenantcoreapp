namespace Hekutenantcoreapp.Application.DTOs;

public class AssignClaimsDto
{
    public IList<PermissionClaimDto> Claims { get; set; } = new List<PermissionClaimDto>();
}
