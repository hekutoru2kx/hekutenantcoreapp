namespace Hekutenantcoreapp.Domain.Models;

public class AuthResult
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();

    public bool MustChangePassword { get; set; }

    public string PreferredTheme { get; set; } = "azure";

    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public IList<TenantSummaryResult> AvailableTenants { get; set; } = new List<TenantSummaryResult>();

    // Mirrors MultiTenantSettings.MultiTenantDisabled at mint time — the frontend uses this to
    // hide the nav-bar "join another tenant" link without a separate round trip.
    public bool MultiTenantDisabled { get; set; }

    // True when registration succeeded but the account is unconfirmed and email confirmation
    // is required — no token is issued and the client should prompt the user to check their
    // inbox instead of treating this as a logged-in session.
    public bool RequiresEmailConfirmation { get; set; }
}