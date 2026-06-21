using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCategoryUexIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_items_id_category_uex_id",
                schema: "sc",
                table: "items",
                columns: new[] { "id_category", "uex_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_items_id_category_uex_id",
                schema: "sc",
                table: "items");
        }
    }
}
