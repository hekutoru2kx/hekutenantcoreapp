using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Hekutenantcoreapp.Application.Resources;
using Microsoft.EntityFrameworkCore;
using Hekutenantcoreapp.Infrastructure.Data;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class UserManagementRepository : IUserManagementRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly HekutenantcoreappDbContext _context;
    private readonly ExportSettings _exportSettings;

    public UserManagementRepository(UserManager<ApplicationUser> userManager, IStringLocalizer<Messages> localizer, HekutenantcoreappDbContext context, ExportSettings exportSettings)
    {
        _userManager = userManager;
        _localizer = localizer;
        _context = context;
        _exportSettings = exportSettings;
    }

    public async Task<UserListResult?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToResult(user, roles);
    }

    public async Task<PagedResult<UserListResult>> GetUsersAsync(UserListQuery query)
    {
        var usersQuery = ApplyFilterAndSort(_userManager.Users.AsQueryable(), query.Search, query.SortBy, query.SortDirection, query.StatusFilter);

        var totalCount = await usersQuery.CountAsync();

        var users = await usersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var result = new List<UserListResult>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(query.RoleFilter) && !roles.Contains(query.RoleFilter))
                continue;

            result.Add(MapToResult(user, roles));
        }

        return new PagedResult<UserListResult>
        {
            Items = result,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IList<UserListResult>> GetAllUsersAsync(string? search, string? sortBy, string? sortDirection, string? roleFilter, bool? statusFilter)
    {
        var usersQuery = ApplyFilterAndSort(_userManager.Users.AsQueryable(), search, sortBy, sortDirection, statusFilter);

        var totalCount = await usersQuery.CountAsync();
        if (!_exportSettings.IsUnlimited && totalCount > _exportSettings.MaxRows)
            throw new Exception(_localizer["ExportTooLarge", _exportSettings.MaxRows!.Value]);

        var users = await usersQuery.ToListAsync();

        var result = new List<UserListResult>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrEmpty(roleFilter) && !roles.Contains(roleFilter))
                continue;

            result.Add(MapToResult(user, roles));
        }

        return result;
    }

    private static IQueryable<ApplicationUser> ApplyFilterAndSort(IQueryable<ApplicationUser> usersQuery, string? search, string? sortBy, string? sortDirection, bool? statusFilter)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            usersQuery = usersQuery.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)));
        }

        if (statusFilter.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.IsActive == statusFilter.Value);
        }

        return sortBy?.ToLower() switch
        {
            "email" => sortDirection == "desc" ? usersQuery.OrderByDescending(u => u.Email) : usersQuery.OrderBy(u => u.Email),
            "createdat" => sortDirection == "desc" ? usersQuery.OrderByDescending(u => u.CreatedAt) : usersQuery.OrderBy(u => u.CreatedAt),
            _ => sortDirection == "desc" ? usersQuery.OrderByDescending(u => u.UserName) : usersQuery.OrderBy(u => u.UserName)
        };
    }

    private static UserListResult MapToResult(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        Email = user.Email ?? string.Empty,
        Roles = roles,
        IsActive = user.IsActive,
        MustChangePassword = user.MustChangePassword,
        CreatedAt = user.CreatedAt,
        DefaultTenantId = user.DefaultTenantId
    };

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.UserName,
            MustChangePassword = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrEmpty(request.Role))
        {
            if (!string.Equals(request.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                throw new Exception(_localizer["OnlyGlobalRoleIsSuperAdmin"]);
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        return new CreateUserResult
        {
            UserId = user.Id,
            Email = request.Email,
            UserName = request.UserName,
            TemporaryPassword = request.Password
        };
    }

    public async Task GrantSuperAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            await _userManager.AddToRoleAsync(user, "SuperAdmin");
    }

    public async Task RevokeSuperAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            return;

        var superAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
        if (superAdmins.Count <= 1)
            throw new Exception(_localizer["CannotRemoveLastSuperAdmin"]);

        await _userManager.RemoveFromRoleAsync(user, "SuperAdmin");
    }

    public async Task DeactivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        user.IsActive = false;
        await _userManager.UpdateAsync(user);
    }

    public async Task ActivateUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        user.IsActive = true;
        await _userManager.UpdateAsync(user);
    }

    public async Task ResetPasswordAsync(string userId, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        user.MustChangePassword = true;
        await _userManager.UpdateAsync(user);
    }

    public async Task SetDefaultTenantAsync(string userId, int? tenantId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        user.DefaultTenantId = tenantId;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new Exception(_localizer["UserNotFound"]);

        var normalizedEmail = user.NormalizedEmail;

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

        if (!string.IsNullOrEmpty(normalizedEmail))
        {
            _context.DeletedAccounts.Add(new DeletedAccount { NormalizedEmail = normalizedEmail });
            await _context.SaveChangesAsync();
        }
    }
}