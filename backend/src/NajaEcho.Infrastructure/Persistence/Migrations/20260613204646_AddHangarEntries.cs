using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHangarEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hangar_entries",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hangar_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_hangar_entries_ship_id",
                        column: x => x.ship_id,
                        principalSchema: "sc",
                        principalTable: "ships",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_hangar_entries_ship_id",
                schema: "sc",
                table: "hangar_entries",
                column: "ship_id");

            migrationBuilder.CreateIndex(
                name: "ix_hangar_entries_user_id",
                schema: "sc",
                table: "hangar_entries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_hangar_entries_user_ship",
                schema: "sc",
                table: "hangar_entries",
                columns: new[] { "user_id", "ship_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hangar_entries",
                schema: "sc");
        }
    }
}
