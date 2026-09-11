using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hekutenantcoreapp.Api.Controllers;

// The one endpoint the generic content layer exposes directly — every write path lives on the
// owning feature's own controller instead (see PersonController / UserController). Authorization
// is delegated to whichever IContentAccessPolicy is registered for the item's OwnerType; an
// owner type with no registered policy is denied by default.
[ApiController]
[Route("api/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly IContentService _contentService;
    private readonly IEnumerable<IContentAccessPolicy> _accessPolicies;

    public ContentController(IContentService contentService, IEnumerable<IContentAccessPolicy> accessPolicies)
    {
        _contentService = contentService;
        _accessPolicies = accessPolicies;
    }

    [HttpGet("{id}/file")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var item = await _contentService.GetByIdAsync(id);
        if (item == null) return NotFound();

        var policy = _accessPolicies.FirstOrDefault(p => p.OwnerType == item.OwnerType);
        if (policy == null || !await policy.CanReadAsync(User, item.OwnerId)) return Forbid();

        try
        {
            var file = await _contentService.OpenFileAsync(id);
            Response.Headers.ETag = $"\"{file.ETag}\"";
            Response.Headers.CacheControl = "private, max-age=86400";
            return File(file.Stream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }
}
