using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Application.Resources;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserManagementRepository _userManagementRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantMembershipRepository _tenantMembershipRepository;
    private readonly IUserTenantRoleRepository _userTenantRoleRepository;
    private readonly ITenantRoleRepository _tenantRoleRepository;
    private readonly IEmailService _emailService;
    private readonly EmailTemplates _emailTemplates;
    private readonly IStringLocalizer<Messages> _localizer;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        IUserManagementRepository userManagementRepository,
        ITenantRepository tenantRepository,
        ITenantMembershipRepository tenantMembershipRepository,
        IUserTenantRoleRepository userTenantRoleRepository,
        ITenantRoleRepository tenantRoleRepository,
        IEmailService emailService,
        EmailTemplates emailTemplates,
        IStringLocalizer<Messages> localizer)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _userManagementRepository = userManagementRepository;
        _tenantRepository = tenantRepository;
        _tenantMembershipRepository = tenantMembershipRepository;
        _userTenantRoleRepository = userTenantRoleRepository;
        _tenantRoleRepository = tenantRoleRepository;
        _emailService = emailService;
        _emailTemplates = emailTemplates;
        _localizer = localizer;
    }

    public async Task<PagedResult<EmployeeResult>> GetEmployeesAsync(EmployeeListQuery query)
    {
        var result = await _employeeRepository.GetEmployeesAsync(query);
        foreach (var employee in result.Items)
            employee.Roles = await _userTenantRoleRepository.GetRoleNamesAsync(employee.UserId, employee.TenantId);
        return result;
    }

    public async Task<IList<EmployeeResult>> GetAllEmployeesAsync(string? search, string? sortBy, string? sortDirection)
    {
        var results = await _employeeRepository.GetAllEmployeesAsync(search, sortBy, sortDirection);
        foreach (var employee in results)
            employee.Roles = await _userTenantRoleRepository.GetRoleNamesAsync(employee.UserId, employee.TenantId);
        return results;
    }

    public async Task<EmployeeResult?> GetEmployeeByIdAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId);
        if (employee == null) return null;

        employee.Roles = await _userTenantRoleRepository.GetRoleNamesAsync(employee.UserId, employee.TenantId);
        return employee;
    }

    public async Task<EmployeeResult> InviteEmployeeAsync(InviteEmployeeRequest request)
    {
        if (string.Equals(request.RoleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            throw new Exception(_localizer["SuperAdminNotTenantAssignable"]);

        var existingUserId = await _userRepository.FindUserIdByEmailAsync(request.Email);
        string userId;
        string? temporaryPassword = null;

        if (existingUserId != null)
        {
            userId = existingUserId;
        }
        else
        {
            temporaryPassword = GenerateTemporaryPassword();
            var createResult = await _userManagementRepository.CreateUserAsync(new CreateUserRequest
            {
                UserName = string.IsNullOrEmpty(request.UserName) ? request.Email : request.UserName,
                Email = request.Email,
                Password = temporaryPassword
            });
            userId = createResult.UserId;
        }

        if (await _employeeRepository.ExistsForUserAsync(userId))
            throw new Exception(_localizer["EmployeeAlreadyExists"]);

        var employee = await _employeeRepository.CreateAsync(userId, request);

        if (!await _tenantMembershipRepository.HasAnyMembershipAsync(userId, employee.TenantId))
            await _tenantMembershipRepository.CreateAsync(userId, employee.TenantId, TenantMembershipStatus.Active);
        else
            await _tenantMembershipRepository.ActivateAsync(userId, employee.TenantId);

        await _userTenantRoleRepository.AssignRolesAsync(userId, employee.TenantId,
            new List<RoleAssignmentRequest> { new() { RoleName = request.RoleName, ExpiresAt = request.RoleExpiresAt } });
        employee.Roles = new List<string> { request.RoleName };

        var tenant = await _tenantRepository.GetTenantByIdAsync(employee.TenantId);
        var (subject, body) = _emailTemplates.TenantInvite(
            string.IsNullOrEmpty(request.UserName) ? request.Email : request.UserName,
            tenant?.Name ?? string.Empty,
            request.RoleName,
            temporaryPassword);
        await _emailService.SendAsync(request.Email, subject, body);

        return employee;
    }

    public async Task UpdateEmployeeAsync(int employeeId, UpsertEmployeeRequest request) =>
        await _employeeRepository.UpdateEmployeeAsync(employeeId, request);

    public async Task<IList<RoleAssignmentResult>> GetRoleAssignmentsAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        return await _userTenantRoleRepository.GetRoleAssignmentsAsync(employee.UserId, employee.TenantId);
    }

    public async Task AssignRolesAsync(int employeeId, IList<RoleAssignmentRequest> assignments)
    {
        if (assignments.Any(a => string.Equals(a.RoleName, "SuperAdmin", StringComparison.OrdinalIgnoreCase)))
            throw new Exception(_localizer["SuperAdminNotTenantAssignable"]);

        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        await _userTenantRoleRepository.AssignRolesAsync(employee.UserId, employee.TenantId, assignments);
    }

    // Role *names* assignable to an employee — deliberately not gated behind RolesPermission
    // (the System-area role-catalog management permission, SuperAdmin-only). A tenant Admin
    // needs to see this list to assign roles within their own tenant via EmployeesPermission.
    // Every role in the shared catalog is assignable except SuperAdmin.
    public async Task<IList<string>> GetAssignableRoleNamesAsync() =>
        await _tenantRoleRepository.GetAssignableRoleNamesAsync();

    public async Task ActivateAsync(int employeeId) =>
        await _employeeRepository.ActivateAsync(employeeId);

    // Deactivation is a staff-side concern only — it must not block the employee's own
    // tenant access outright (they may also be a patient there). Revoking their currently
    // held role assignments means their operational claims stop being minted on the next
    // login/tenant-switch, while ActiveUserMiddleware still lets them in via TenantMembership.
    public async Task DeactivateAsync(int employeeId)
    {
        var employee = await _employeeRepository.GetEmployeeByIdAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        await _employeeRepository.DeactivateAsync(employeeId);
        await _userTenantRoleRepository.RevokeAllAsync(employee.UserId, employee.TenantId);
    }

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%";
        var random = new Random();
        var password = new string(Enumerable.Range(0, 12)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
        return password + "A1!";
    }
}
