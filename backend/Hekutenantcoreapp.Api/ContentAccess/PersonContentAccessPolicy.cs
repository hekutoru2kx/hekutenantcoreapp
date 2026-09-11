using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Constants;
using Hekutenantcoreapp.Domain.Enums.Permissions;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hekutenantcoreapp.Api.ContentAccess;

// Registered against ContentOwnerTypes.Person so the shared GET /api/content/{id}/file endpoint
// can authorize a read without knowing anything about Person. A caller can read their own
// profile picture, or anyone's with PersonsPermission.Read. Note this check only needs to
// compare ids — it never needs to check tenant membership itself, because GetByIdAsync's
// underlying query is already tenant-filtered (ContentItem/Person are both ITenantScoped): a
// cross-tenant id simply won't resolve to an item at all, before this policy ever runs.
public class PersonContentAccessPolicy : IContentAccessPolicy
{
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authorizationService;

    public PersonContentAccessPolicy(IUserService userService, IAuthorizationService authorizationService)
    {
        _userService = userService;
        _authorizationService = authorizationService;
    }

    public string OwnerType => ContentOwnerTypes.Person;

    public async Task<bool> CanReadAsync(ClaimsPrincipal user, int ownerId)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            var person = await _userService.GetPersonAsync(userId);
            if (person != null && person.Id == ownerId) return true;
        }

        var authResult = await _authorizationService.AuthorizeAsync(user,
            nameof(PersonsPermission) + "." + nameof(PersonsPermission.Read));
        return authResult.Succeeded;
    }
}
