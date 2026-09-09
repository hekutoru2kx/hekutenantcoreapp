using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Application.Resources;

namespace Hekutenantcoreapp.Application.Services;

public class EmailTemplates
{
    private readonly IStringLocalizer<Messages> _localizer;

    public EmailTemplates(IStringLocalizer<Messages> localizer)
    {
        _localizer = localizer;
    }

    public (string Subject, string Body) Welcome(string fullName)
    {
        var subject = _localizer["EmailWelcomeSubject"];
        var title = string.Format(_localizer["EmailWelcomeTitle"], fullName);
        var body = _localizer["EmailWelcomeBody"];

        var html = $@"
            <h2>{title}</h2>
            <p>{body}</p>
        ";

        return (subject, html);
    }

    public (string Subject, string Body) ConfirmEmail(string fullName, string confirmationUrl)
    {
        var subject = _localizer["EmailConfirmSubject"];
        var title = string.Format(_localizer["EmailConfirmTitle"], fullName);
        var body = _localizer["EmailConfirmBody"];
        var button = _localizer["EmailConfirmButton"];

        var html = $@"
            <h2>{title}</h2>
            <p>{body}</p>
            <p><a href=""{confirmationUrl}"" style=""display:inline-block;padding:10px 18px;background:#1976d2;color:#ffffff;text-decoration:none;border-radius:4px;"">{button}</a></p>
            <p style=""font-size:12px;color:#666666;word-break:break-all;"">{confirmationUrl}</p>
        ";

        return (subject, html);
    }

    public (string Subject, string Body) PasswordChanged(string fullName)
    {
        var subject = _localizer["EmailPasswordChangedSubject"];
        var title = _localizer["EmailPasswordChangedTitle"];
        var body = string.Format(_localizer["EmailPasswordChangedBody"], fullName);

        var html = $@"
            <h2>{title}</h2>
            <p>{body}</p>
        ";

        return (subject, html);
    }

    public (string Subject, string Body) PasswordReset(string fullName, string temporaryPassword)
    {
        var subject = _localizer["EmailPasswordResetSubject"];
        var title = _localizer["EmailPasswordResetTitle"];
        var body = string.Format(_localizer["EmailPasswordResetBody"], fullName);
        var footer = _localizer["EmailPasswordResetFooter"];

        var html = $@"
            <h2>{title}</h2>
            <p>{body}</p>
            <p style=""font-size: 18px; font-weight: bold;"">{temporaryPassword}</p>
            <p>{footer}</p>
        ";

        return (subject, html);
    }

    public (string Subject, string Body) TenantInvite(string fullName, string tenantName, string roleName, string? temporaryPassword)
    {
        var subject = string.Format(_localizer["EmailTenantInviteSubject"], tenantName);
        var title = string.Format(_localizer["EmailTenantInviteTitle"], tenantName);
        var body = temporaryPassword != null
            ? string.Format(_localizer["EmailTenantInviteBodyWithPassword"], fullName, tenantName, roleName)
            : string.Format(_localizer["EmailTenantInviteBody"], fullName, tenantName, roleName);

        var html = temporaryPassword != null
            ? $@"
                <h2>{title}</h2>
                <p>{body}</p>
                <p style=""font-size: 18px; font-weight: bold;"">{temporaryPassword}</p>
            "
            : $@"
                <h2>{title}</h2>
                <p>{body}</p>
            ";

        return (subject, html);
    }
}