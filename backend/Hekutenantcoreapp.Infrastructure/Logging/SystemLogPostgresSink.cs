using Hekutenantcoreapp.Domain.Enums;
using Npgsql;
using Serilog.Core;
using Serilog.Events;
using System.Threading.Channels;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// Writes log events to the system_logs table via its own NpgsqlConnection — deliberately NOT
// through HekutenantcoreappDbContext/EF, so a broken DbContext or exhausted EF pool isn't also
// what breaks logging. Log calls never block on a DB round-trip: Emit() only queues; a background
// loop drains and batches the writes. If the channel fills (DB slow/down) the oldest queued event
// is dropped rather than the caller being blocked — a struggling DB must never back-pressure the
// app. Any write failure is caught and swallowed here; the console/file sinks configured alongside
// this one are the fallback of record for exactly that case (this sink must never be the only sink).
public class SystemLogPostgresSink : ILogEventSink, IDisposable
{
    private readonly Channel<LogEvent> _channel;
    private readonly string _connectionString;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _pumpTask;

    public SystemLogPostgresSink(string connectionString)
    {
        _connectionString = connectionString;
        _channel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(2000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
        _pumpTask = Task.Run(() => PumpAsync(_cts.Token));
    }

    public void Emit(LogEvent logEvent) => _channel.Writer.TryWrite(logEvent);

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        var batch = new List<LogEvent>(100);
        while (!cancellationToken.IsCancellationRequested)
        {
            batch.Clear();
            try
            {
                if (!await _channel.Reader.WaitToReadAsync(cancellationToken))
                    continue;

                while (batch.Count < 100 && _channel.Reader.TryRead(out var evt))
                    batch.Add(evt);

                if (batch.Count > 0)
                    await WriteBatchAsync(batch, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Swallow — a DB outage must not kill the pump loop or throw back into the app.
            }
        }
    }

    private async Task WriteBatchAsync(List<LogEvent> batch, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var evt in batch)
        {
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = """
                INSERT INTO system_logs (timestamp, level, category, message, exception, tenant_id, user_id, trace_id)
                VALUES (@timestamp, @level, @category, @message, @exception, @tenant_id, @user_id, @trace_id)
                """;
            cmd.Parameters.AddWithValue("timestamp", evt.Timestamp.UtcDateTime);
            cmd.Parameters.AddWithValue("level", evt.Level.ToString());
            cmd.Parameters.AddWithValue("category", GetStringProperty(evt, "LogCategory") ?? LogCategory.System.ToString());
            cmd.Parameters.AddWithValue("message", evt.RenderMessage());
            cmd.Parameters.AddWithValue("exception", (object?)evt.Exception?.ToString() ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tenant_id", (object?)GetIntProperty(evt, "TenantId") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("user_id", (object?)GetStringProperty(evt, "UserId") ?? DBNull.Value);
            cmd.Parameters.AddWithValue("trace_id", (object?)GetStringProperty(evt, "TraceId") ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static string? GetStringProperty(LogEvent evt, string name) =>
        evt.Properties.TryGetValue(name, out var value) && value is ScalarValue scalar
            ? scalar.Value?.ToString()
            : null;

    private static int? GetIntProperty(LogEvent evt, string name) =>
        evt.Properties.TryGetValue(name, out var value) && value is ScalarValue { Value: int intValue }
            ? intValue
            : null;

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        _cts.Cancel();
        try { _pumpTask.Wait(TimeSpan.FromSeconds(5)); }
        catch { /* best-effort drain on shutdown */ }
        _cts.Dispose();
    }
}
