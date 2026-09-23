using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface ISystemLogsRepository
{
    Task<SystemLogsPageResult> QueryAsync(SystemLogsQuery query);
}
