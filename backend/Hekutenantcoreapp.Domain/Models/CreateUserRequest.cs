namespace Hekutenantcoreapp.Domain.Models;

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }

    // When false, the created account starts unconfirmed and (if the app requires email
    // confirmation) the user must click the emailed link before they can log in. Defaults to
    // true so admin-provisioned and other non-self-service creations are unaffected.
    public bool EmailConfirmed { get; set; } = true;
}