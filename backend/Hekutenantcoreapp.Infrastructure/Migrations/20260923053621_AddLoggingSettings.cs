using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hekutenantcoreapp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoggingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "logging_category_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    category = table.Column<string>(type: "text", nullable: false),
                    minimum_level = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logging_category_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "logging_retention_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    retention_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logging_retention_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_logging_category_settings_category",
                table: "logging_category_settings",
                column: "category",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_logging_category_settings_created_at",
                table: "logging_category_settings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_logging_category_settings_updated_at",
                table: "logging_category_settings",
                column: "updated_at");

            migrationBuilder.CreateIndex(
                name: "IX_logging_retention_settings_created_at",
                table: "logging_retention_settings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_logging_retention_settings_updated_at",
                table: "logging_retention_settings",
                column: "updated_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "logging_category_settings");

            migrationBuilder.DropTable(
                name: "logging_retention_settings");
        }
    }
}
