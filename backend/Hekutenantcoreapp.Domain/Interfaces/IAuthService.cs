namespace Hekutenantcoreapp.Domain.Interfaces;

using Hekutenantcoreapp.Domain.Models;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(string email, string password);
    Task AssignRoleAsync(string email, string role);
    Task<AuthResult> LoginOrRegisterWithGoogleAsync(string idToken, int? tenantId);
    Task<AuthResult> SelectTenantAsync(string userId, int tenantId);
    Task<AuthResult> JoinTenantAsPatientAsync(string userId, int tenantId);
    Task<IList<TenantSummaryResult>> GetAvailableTenantsAsync(string userId);
}