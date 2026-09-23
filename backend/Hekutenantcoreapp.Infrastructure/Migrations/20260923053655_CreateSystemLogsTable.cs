using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hekutenantcoreapp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateSystemLogsTable : Migration
    {
        // system_logs is intentionally NOT part of the EF model (no DbSet on
        // HekutenantcoreappDbContext, no entity/configuration class) — it's written by
        // SystemLogPostgresSink and read by the SuperAdmin log viewer, both via raw Npgsql, never
        // through EF change tracking. Hand-written SQL here, same convention gestamind uses for
        // this exact table.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE system_logs (
                    id bigserial PRIMARY KEY,
                    timestamp timestamp without time zone NOT NULL,
                    level varchar(20) NOT NULL,
                    category varchar(20) NOT NULL,
                    message text NOT NULL,
                    exception text NULL,
                    tenant_id integer NULL,
                    user_id text NULL,
                    trace_id text NULL
                );

                CREATE INDEX ix_system_logs_filter ON system_logs (tenant_id, category, level, timestamp);
                CREATE INDEX ix_system_logs_timestamp ON system_logs (timestamp);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS system_logs;");
        }
    }
}
