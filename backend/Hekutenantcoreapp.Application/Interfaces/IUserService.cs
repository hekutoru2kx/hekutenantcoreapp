using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IUserService
{
    Task UpdateLanguageAsync(string userId, string language);
    Task<UserProfileResult> GetProfileAsync(string userId);
    Task UpdateProfileAsync(UpdateProfileRequest request);
    Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<PersonResult?> GetPersonAsync(string userId);
    Task UpsertPersonAsync(string userId, UpsertPersonRequest request);
    Task<PersonMatchResult> CheckExistingPersonAsync(string callerId, string? documentType, string? documentId, string? email);
}