using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Hekutenantcoreapp.Infrastructure.Repositories;

// Reads system_logs directly via Npgsql, same as SystemLogPostgresSink writes it — the table is
// deliberately outside the EF model (see the CreateSystemLogsTable migration), so this is raw SQL
// rather than an EF query. SuperAdmin-only (gated by LoggingSettingsPermission.Read at the
// controller), which is what makes the unrestricted TenantId filter here safe — everywhere else in
// this app, tenant scoping is automatic and mandatory (HekutenantcoreappDbContext's query filters);
// this is the one deliberate exception, because seeing every tenant's logs is the whole point.
public class SystemLogsRepository : ISystemLogsRepository
{
    private readonly string _connectionString;

    public SystemLogsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<SystemLogsPageResult> QueryAsync(SystemLogsQuery query)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var page = Math.Max(query.Page, 1);

        var whereClauses = new List<string>();
        var parameterValues = new List<(string Name, object Value)>();

        if (query.TenantId is not null)
        {
            whereClauses.Add("tenant_id = @tenantId");
            parameterValues.Add(("tenantId", query.TenantId.Value));
        }
        if (!string.IsNullOrEmpty(query.Category))
        {
            whereClauses.Add("category = @category");
            parameterValues.Add(("category", query.Category));
        }
        if (!string.IsNullOrEmpty(query.Level))
        {
            whereClauses.Add("level = @level");
            parameterValues.Add(("level", query.Level));
        }
        if (query.From is not null)
        {
            whereClauses.Add("timestamp >= @from");
            parameterValues.Add(("from", query.From.Value));
        }
        if (query.To is not null)
        {
            whereClauses.Add("timestamp <= @to");
            parameterValues.Add(("to", query.To.Value));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        var result = new SystemLogsPageResult();

        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = $"SELECT COUNT(*) FROM system_logs {whereSql}";
            foreach (var (name, value) in parameterValues)
                countCmd.Parameters.Add(new NpgsqlParameter(name, value));
            result.TotalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        }

        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"""
                SELECT id, timestamp, level, category, message, exception, tenant_id, user_id, trace_id
                FROM system_logs
                {whereSql}
                ORDER BY timestamp DESC
                LIMIT @pageSize OFFSET @offset
                """;
            foreach (var (name, value) in parameterValues)
                cmd.Parameters.Add(new NpgsqlParameter(name, value));
            cmd.Parameters.Add(new NpgsqlParameter("pageSize", pageSize));
            cmd.Parameters.Add(new NpgsqlParameter("offset", (page - 1) * pageSize));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Items.Add(new SystemLogEntryResult
                {
                    Id = reader.GetInt64(0),
                    Timestamp = reader.GetDateTime(1),
                    Level = reader.GetString(2),
                    Category = reader.GetString(3),
                    Message = reader.GetString(4),
                    Exception = reader.IsDBNull(5) ? null : reader.GetString(5),
                    TenantId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    UserId = reader.IsDBNull(7) ? null : reader.GetString(7),
                    TraceId = reader.IsDBNull(8) ? null : reader.GetString(8)
                });
            }
        }

        return result;
    }
}
