using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IUserRepository
{
    Task<string> CreateUserAsync(CreateUserRequest request);
    Task<string?> ValidateUserAsync(string email, string password);
    Task<string?> FindUserIdByEmailAsync(string email);
    Task<(string UserName, IList<string> Roles, bool MustChangePassword, string PreferredTheme, int? DefaultTenantId)> GetUserInfoAsync(string userId);
    Task UpdateLanguageAsync(string userId, string language);
    Task UpdateProfileAsync(UpdateProfileRequest request);
    Task AssignRoleAsync(string email, string role);
    Task<IList<string>> GetRolesAsync(string userId);
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<UserProfileResult> GetProfileAsync(string userId);

    Task<PersonResult?> GetPersonAsync(string userId);
    Task UpsertPersonAsync(string userId, UpsertPersonRequest request);
    Task<PersonMatchResult> CheckExistingPersonAsync(string callerId, string? documentType, string? documentId, string? email);

    Task<(string UserId, bool IsNewUser)> FindOrCreateGoogleUserAsync(GoogleUserInfo googleUser);
}