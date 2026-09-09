using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hekutenantcoreapp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    require_email_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_created_at",
                table: "app_settings",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_app_settings_updated_at",
                table: "app_settings",
                column: "updated_at");

            // Grandfather every existing account as confirmed. Registration never set
            // EmailConfirmed before this feature, so accounts created up to now carry the
            // Identity default of false; without this, turning on RequireEmailConfirmation
            // later would lock out real users. New registrations from here on are gated.
            migrationBuilder.Sql(
                "UPDATE \"AspNetUsers\" SET \"EmailConfirmed\" = true WHERE \"EmailConfirmed\" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "app_settings");
        }
    }
}
