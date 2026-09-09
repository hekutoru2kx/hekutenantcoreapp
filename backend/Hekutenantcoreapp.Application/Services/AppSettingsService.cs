using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly IAppSettingsRepository _repository;

    public AppSettingsService(IAppSettingsRepository repository)
    {
        _repository = repository;
    }

    public Task<AppSettingsResult> GetSettingsAsync() => _repository.GetSettingsAsync();

    public Task UpdateSettingsAsync(UpdateAppSettingsRequest request) => _repository.UpdateSettingsAsync(request);
}
