using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Hekutenantcoreapp.Domain.Models;
using Hekutenantcoreapp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

// Reads/updates the single MultiTenantSettings row — MultiTenantSettingsSeeder guarantees it
// exists at startup, so GetSettingsAsync never returns null in practice.
public class MultiTenantSettingsRepository : IMultiTenantSettingsRepository
{
    private readonly HekutenantcoreappDbContext _context;
    private readonly IStringLocalizer<Messages> _localizer;

    public MultiTenantSettingsRepository(HekutenantcoreappDbContext context, IStringLocalizer<Messages> localizer)
    {
        _context = context;
        _localizer = localizer;
    }

    public async Task<MultiTenantSettingsResult> GetSettingsAsync()
    {
        var settings = await _context.MultiTenantSettings.Include(s => s.DefaultTenant).FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["MultiTenantSettingsNotFound"]);

        return MapToResult(settings);
    }

    public async Task UpdateSettingsAsync(UpdateMultiTenantSettingsRequest request)
    {
        var settings = await _context.MultiTenantSettings.FirstOrDefaultAsync()
            ?? throw new Exception(_localizer["MultiTenantSettingsNotFound"]);

        settings.DefaultTenantLoginEnabled = request.DefaultTenantLoginEnabled;
        settings.MultiTenantDisabled = request.MultiTenantDisabled;
        settings.DefaultTenantId = request.DefaultTenantId;

        await _context.SaveChangesAsync();
    }

    private static MultiTenantSettingsResult MapToResult(Domain.Entities.MultiTenantSettings settings) => new()
    {
        DefaultTenantLoginEnabled = settings.DefaultTenantLoginEnabled,
        MultiTenantDisabled = settings.MultiTenantDisabled,
        DefaultTenantId = settings.DefaultTenantId,
        DefaultTenantName = settings.DefaultTenant?.Name
    };
}
