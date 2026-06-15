using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseItemQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quality",
                table: "warehouse_inventory",
                type: "integer",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddCheckConstraint(
                name: "ck_warehouse_inventory_quality",
                table: "warehouse_inventory",
                sql: "quality >= 1 AND quality <= 1000");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_warehouse_inventory_quality",
                table: "warehouse_inventory");

            migrationBuilder.DropColumn(
                name: "quality",
                table: "warehouse_inventory");
        }
    }
}
