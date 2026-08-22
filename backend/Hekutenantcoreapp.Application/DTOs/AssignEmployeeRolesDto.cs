namespace Hekutenantcoreapp.Application.DTOs;

public class AssignEmployeeRolesDto
{
    public IList<RoleAssignmentDto> Roles { get; set; } = new List<RoleAssignmentDto>();
}
