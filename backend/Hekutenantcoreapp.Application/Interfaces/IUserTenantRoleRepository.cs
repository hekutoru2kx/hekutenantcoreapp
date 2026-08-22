using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface IUserTenantRoleRepository
{
    // Currently open (non-revoked), started, and non-expired role names only — this is what
    // feeds JWT minting.
    Task<IList<string>> GetRoleNamesAsync(string userId, int tenantId);

    // Every currently open (non-revoked) assignment, including ones past their own ExpiresAt
    // or not yet past their own StartsAt, for the management page. Revoked assignments are
    // kept in the table for history but excluded here — they're no longer part of the
    // "current state" being edited.
    Task<IList<RoleAssignmentResult>> GetRoleAssignmentsAsync(string userId, int tenantId);

    // Diffs against the open assignments: a role kept in the new set is updated in place
    // (preserving CreatedAt/CreatedBy — it's an edit, not a new grant), a role dropped from
    // the set is soft-revoked, and a role not currently held opens a fresh row (so re-granting
    // after a rehire doesn't collide with the filtered unique index and history is preserved).
    Task AssignRolesAsync(string userId, int tenantId, IList<RoleAssignmentRequest> assignments);

    // Soft-closes every currently open assignment without replacing them — used when an
    // employee is deactivated, so their operational role claims stop being minted on the
    // next login/tenant-switch without erasing the history of what they held.
    Task RevokeAllAsync(string userId, int tenantId);
}
