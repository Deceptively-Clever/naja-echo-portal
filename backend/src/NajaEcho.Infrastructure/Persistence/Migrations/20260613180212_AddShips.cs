using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    uuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    name_full = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    company_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ships", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ships_status",
                table: "ships",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_ships_uex_id",
                table: "ships",
                column: "uex_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ships");
        }
    }
}
