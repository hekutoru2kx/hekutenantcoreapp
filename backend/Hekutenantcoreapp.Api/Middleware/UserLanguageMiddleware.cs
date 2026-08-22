using Hekutenantcoreapp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Globalization;

namespace Hekutenantcoreapp.Api.Middleware;

public class UserLanguageMiddleware
{
    private readonly RequestDelegate _next;

    public UserLanguageMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null && !string.IsNullOrEmpty(user.PreferredLanguage))
            {
                var culture = new CultureInfo(user.PreferredLanguage);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
            }
        }

        await _next(context);
    }
}