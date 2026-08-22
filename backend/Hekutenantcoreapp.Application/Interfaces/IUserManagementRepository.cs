using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IUserManagementRepository
{
    Task<PagedResult<UserListResult>> GetUsersAsync(UserListQuery query);
    Task<IList<UserListResult>> GetAllUsersAsync(string? search, string? sortBy, string? sortDirection, string? roleFilter, bool? statusFilter);
    Task<UserListResult?> GetUserByIdAsync(string userId);
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request);
    Task GrantSuperAdminAsync(string userId);
    Task RevokeSuperAdminAsync(string userId);
    Task DeactivateUserAsync(string userId);
    Task ActivateUserAsync(string userId);
    Task ResetPasswordAsync(string userId, string newPassword);
    Task DeleteUserAsync(string userId);
    Task SetDefaultTenantAsync(string userId, int? tenantId);
}