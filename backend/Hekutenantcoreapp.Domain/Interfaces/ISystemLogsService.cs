using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Domain.Interfaces;

public interface ISystemLogsService
{
    Task<SystemLogsPageResult> QueryAsync(SystemLogsQuery query);
}
