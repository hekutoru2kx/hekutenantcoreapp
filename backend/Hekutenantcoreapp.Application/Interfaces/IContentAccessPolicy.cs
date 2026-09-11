using System.Security.Claims;

namespace Hekutenantcoreapp.Application.Interfaces;

// Registered per owner type by the feature that owns that entity, so the one shared download
// endpoint (GET /api/content/{id}/file) can delegate its authorization check without a generic
// controller needing to know every owner's permission model. No generic implementation exists —
// each owner type must register its own (see PersonContentAccessPolicy).
public interface IContentAccessPolicy
{
    string OwnerType { get; }

    Task<bool> CanReadAsync(ClaimsPrincipal user, int ownerId);
}
