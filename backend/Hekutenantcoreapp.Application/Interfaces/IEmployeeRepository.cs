using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<PagedResult<EmployeeResult>> GetEmployeesAsync(EmployeeListQuery query);
    Task<IList<EmployeeResult>> GetAllEmployeesAsync(string? search, string? sortBy, string? sortDirection);
    Task<EmployeeResult?> GetEmployeeByIdAsync(int employeeId);
    Task<bool> ExistsForUserAsync(string userId);
    Task<EmployeeResult> CreateAsync(string userId, InviteEmployeeRequest request);
    Task UpdateEmployeeAsync(int employeeId, UpsertEmployeeRequest request);
    Task ActivateAsync(int employeeId);
    Task DeactivateAsync(int employeeId);
}
