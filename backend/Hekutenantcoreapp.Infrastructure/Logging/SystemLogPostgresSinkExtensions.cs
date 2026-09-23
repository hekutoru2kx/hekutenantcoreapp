using Serilog;
using Serilog.Configuration;

namespace Hekutenantcoreapp.Infrastructure.Logging;

public static class SystemLogPostgresSinkExtensions
{
    public static LoggerConfiguration SystemLogPostgres(this LoggerSinkConfiguration sinkConfiguration, string connectionString) =>
        sinkConfiguration.Sink(new SystemLogPostgresSink(connectionString));
}
