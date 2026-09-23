using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// The sole producer of Database-category log content — EF's own .LogTo()/EnableSensitiveDataLogging
// stay unused/unwired everywhere else, so a query is never logged twice through two different
// paths. Always logs command text + duration at Debug (Warning if it crosses the slow-query
// threshold); parameter values are included only in Development (PHI risk otherwise), and even
// then never for a handful of Identity columns that must never appear in a log at all.
public class QueryLoggingInterceptor : DbCommandInterceptor
{
    private static readonly TimeSpan SlowQueryThreshold = TimeSpan.FromMilliseconds(500);

    private static readonly HashSet<string> SensitiveParameterColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "PasswordHash", "SecurityStamp", "ConcurrencyStamp"
    };

    private readonly ICategoryLogger _categoryLogger;
    private readonly bool _includeParameterValues;

    public QueryLoggingInterceptor(ICategoryLogger categoryLogger, IHostEnvironment environment)
    {
        _categoryLogger = categoryLogger;
        _includeParameterValues = environment.IsDevelopment();
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Log(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Log(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Log(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Log(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void Log(DbCommand command, CommandExecutedEventData eventData)
    {
        var isSlow = eventData.Duration > SlowQueryThreshold;
        var level = isSlow ? LogLevel.Warning : LogLevel.Debug;

        var message = _includeParameterValues
            ? $"{command.CommandText} [{FormatParameters(command)}] ({eventData.Duration.TotalMilliseconds:F1}ms)"
            : $"{command.CommandText} ({eventData.Duration.TotalMilliseconds:F1}ms)";

        _categoryLogger.Log(LogCategory.Database, level, message);
    }

    private static string FormatParameters(DbCommand command)
    {
        var parts = new List<string>(command.Parameters.Count);
        foreach (DbParameter parameter in command.Parameters)
        {
            var columnName = parameter.ParameterName.TrimStart('@');
            var value = SensitiveParameterColumns.Contains(columnName) ? "***REDACTED***" : parameter.Value?.ToString() ?? "null";
            parts.Add($"{parameter.ParameterName}={value}");
        }
        return string.Join(", ", parts);
    }
}
