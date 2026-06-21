using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveShipsToScSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sc");

            migrationBuilder.RenameTable(
                name: "ships",
                newName: "ships",
                newSchema: "sc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ships",
                schema: "sc",
                newName: "ships");
        }
    }
}
