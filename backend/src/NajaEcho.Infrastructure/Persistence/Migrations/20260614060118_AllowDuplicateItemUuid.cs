using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateItemUuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_items_uuid",
                schema: "sc",
                table: "items");

            migrationBuilder.CreateIndex(
                name: "ix_items_uuid",
                schema: "sc",
                table: "items",
                column: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_items_uuid",
                schema: "sc",
                table: "items");

            migrationBuilder.CreateIndex(
                name: "ix_items_uuid",
                schema: "sc",
                table: "items",
                column: "uuid",
                unique: true);
        }
    }
}
