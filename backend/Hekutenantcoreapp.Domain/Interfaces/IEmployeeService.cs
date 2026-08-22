using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface IEmployeeService
{
    Task<PagedResult<EmployeeResult>> GetEmployeesAsync(EmployeeListQuery query);
    Task<IList<EmployeeResult>> GetAllEmployeesAsync(string? search, string? sortBy, string? sortDirection);
    Task<EmployeeResult?> GetEmployeeByIdAsync(int employeeId);
    Task<EmployeeResult> InviteEmployeeAsync(InviteEmployeeRequest request);
    Task UpdateEmployeeAsync(int employeeId, UpsertEmployeeRequest request);
    Task<IList<RoleAssignmentResult>> GetRoleAssignmentsAsync(int employeeId);
    Task<IList<string>> GetAssignableRoleNamesAsync();
    Task AssignRolesAsync(int employeeId, IList<RoleAssignmentRequest> assignments);
    Task ActivateAsync(int employeeId);
    Task DeactivateAsync(int employeeId);
}
