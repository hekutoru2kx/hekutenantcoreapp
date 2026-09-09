using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Entities;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly UserManager<Identity.ApplicationUser> _userManager;
    private readonly IStringLocalizer<Messages> _localizer;
    private readonly ExportSettings _exportSettings;

    public EmployeeRepository(HekutenantcoreappDbContext context, UserManager<Identity.ApplicationUser> userManager, IStringLocalizer<Messages> localizer, ExportSettings exportSettings)
    {
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _exportSettings = exportSettings;
    }

    public async Task<PagedResult<EmployeeResult>> GetEmployeesAsync(EmployeeListQuery query)
    {
        var employeesQuery = ApplyFilterAndSort(_context, _context.Employees.AsQueryable(), query.Search, query.SortBy, query.SortDirection);

        var totalCount = await employeesQuery.CountAsync();

        var employees = await employeesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var results = new List<EmployeeResult>();
        foreach (var employee in employees)
            results.Add(await MapToResultAsync(employee));

        return new PagedResult<EmployeeResult>
        {
            Items = results,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<IList<EmployeeResult>> GetAllEmployeesAsync(string? search, string? sortBy, string? sortDirection)
    {
        var employeesQuery = ApplyFilterAndSort(_context, _context.Employees.AsQueryable(), search, sortBy, sortDirection);

        var totalCount = await employeesQuery.CountAsync();
        if (!_exportSettings.IsUnlimited && totalCount > _exportSettings.MaxRows)
            throw new Exception(_localizer["ExportTooLarge", _exportSettings.MaxRows!.Value]);

        var employees = await employeesQuery.ToListAsync();

        var results = new List<EmployeeResult>();
        foreach (var employee in employees)
            results.Add(await MapToResultAsync(employee));

        return results;
    }

    // Automatically scoped to the caller's current tenant via the ITenantScoped query filter.
    private static IQueryable<Employee> ApplyFilterAndSort(HekutenantcoreappDbContext context, IQueryable<Employee> employeesQuery, string? search, string? sortBy, string? sortDirection)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            employeesQuery = employeesQuery.Where(e =>
                context.Users.Any(u => u.Id == e.UserId && (
                    (u.UserName != null && u.UserName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term))))
                || (e.JobTitle != null && e.JobTitle.ToLower().Contains(term)));
        }

        return sortBy?.ToLower() switch
        {
            "hiredate" => sortDirection == "desc"
                ? employeesQuery.OrderByDescending(e => e.HireDate)
                : employeesQuery.OrderBy(e => e.HireDate),
            _ => sortDirection == "desc"
                ? employeesQuery.OrderByDescending(e => e.JobTitle)
                : employeesQuery.OrderBy(e => e.JobTitle)
        };
    }

    public async Task<EmployeeResult?> GetEmployeeByIdAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        return employee == null ? null : await MapToResultAsync(employee);
    }

    public async Task<bool> ExistsForUserAsync(string userId)
    {
        return await _context.Employees.AnyAsync(e => e.UserId == userId);
    }

    public async Task<EmployeeResult> CreateAsync(string userId, InviteEmployeeRequest request)
    {
        var employee = new Employee
        {
            UserId = userId,
            JobTitle = request.JobTitle,
            HireDate = request.HireDate,
            IsActive = true
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return await MapToResultAsync(employee);
    }

    public async Task UpdateEmployeeAsync(int employeeId, UpsertEmployeeRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        employee.JobTitle = request.JobTitle;
        employee.HireDate = request.HireDate;
        employee.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task ActivateAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        employee.IsActive = true;
        await _context.SaveChangesAsync();
    }

    public async Task DeactivateAsync(int employeeId)
    {
        var employee = await _context.Employees.FindAsync(employeeId)
            ?? throw new Exception(_localizer["EmployeeNotFound"]);

        employee.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private async Task<EmployeeResult> MapToResultAsync(Employee employee)
    {
        var user = await _userManager.FindByIdAsync(employee.UserId);
        // Employee roles are tenant-scoped (UserTenantRole), not global AspNetUserRoles —
        // the service layer fills Roles in via IUserTenantRoleRepository after mapping.

        return new EmployeeResult
        {
            Id = employee.Id,
            TenantId = employee.TenantId,
            UserId = employee.UserId,
            UserName = user?.UserName ?? string.Empty,
            Email = user?.Email ?? string.Empty,
            JobTitle = employee.JobTitle,
            HireDate = employee.HireDate,
            IsActive = employee.IsActive,
            Roles = new List<string>()
        };
    }
}
