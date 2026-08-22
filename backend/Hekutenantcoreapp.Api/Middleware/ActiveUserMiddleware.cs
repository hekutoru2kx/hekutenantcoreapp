using Hekutenantcoreapp.Infrastructure.Data;
using Hekutenantcoreapp.Infrastructure.Identity;
using Hekutenantcoreapp.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Hekutenantcoreapp.Api.Middleware;

public class ActiveUserMiddleware
{
    private readonly RequestDelegate _next;

    public ActiveUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager, HekutenantcoreappDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { message = "Account is deactivated." });
                    return;
                }

                // Tenant-level revocation: a SuperAdmin bypasses membership checks entirely.
                // Everyone else with a tenant selected must still have a non-suspended
                // membership in that tenant — enforced live per-request so suspension takes
                // effect immediately rather than waiting up to 8h for the token to expire.
                //
                // Employee.IsActive is deliberately NOT checked here: a deactivated employee
                // may still be a patient of the same tenant, and losing staff status must not
                // block their own tenant access outright. Deactivation instead revokes their
                // UserTenantRole assignments (see EmployeeService.DeactivateAsync), so their
                // operational claims simply stop being minted on the next login/tenant-switch —
                // the same "takes effect on next token" trade-off role-claim edits already have.
                var isSuperAdmin = await userManager.IsInRoleAsync(user, "SuperAdmin");
                var tenantIdClaim = context.User.FindFirstValue("tenant_id");
                if (!isSuperAdmin && int.TryParse(tenantIdClaim, out var tenantId))
                {
                    var membershipOk = await db.TenantMemberships
                        .IgnoreQueryFilters()
                        .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && m.Status != TenantMembershipStatus.Suspended);

                    if (!membershipOk)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { message = "Access to this tenant has been revoked." });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
