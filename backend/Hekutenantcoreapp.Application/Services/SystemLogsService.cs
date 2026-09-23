using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Interfaces;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Services;

public class SystemLogsService : ISystemLogsService
{
    private readonly ISystemLogsRepository _repository;

    public SystemLogsService(ISystemLogsRepository repository)
    {
        _repository = repository;
    }

    public async Task<SystemLogsPageResult> QueryAsync(SystemLogsQuery query) =>
        await _repository.QueryAsync(query);
}
