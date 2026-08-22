using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class DatabaseKeepAliveService : IDatabaseKeepAliveService
{
    private readonly IDatabaseKeepAliveRepository _repository;

    public DatabaseKeepAliveService(IDatabaseKeepAliveRepository repository)
    {
        _repository = repository;
    }

    public async Task<DatabaseKeepAliveSettingsResult> GetSettingsAsync() =>
        await _repository.GetSettingsAsync();

    public async Task UpdateSettingsAsync(UpdateDatabaseKeepAliveSettingsRequest request) =>
        await _repository.UpdateSettingsAsync(request);
}
