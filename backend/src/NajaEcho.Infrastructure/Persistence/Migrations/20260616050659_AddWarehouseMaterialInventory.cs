using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseMaterialInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "warehouse_material_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    commodity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quality = table.Column<int>(type: "integer", nullable: false, defaultValue: 500),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_material_inventory", x => x.id);
                    table.CheckConstraint("ck_warehouse_material_inventory_quality", "quality >= 1 AND quality <= 1000");
                    table.CheckConstraint("ck_warehouse_material_inventory_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_warehouse_material_inventory_commodity_id",
                        column: x => x.commodity_id,
                        principalSchema: "sc",
                        principalTable: "commodities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_material_inventory_commodity_id",
                table: "warehouse_material_inventory",
                column: "commodity_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_material_inventory_owner_user_id",
                table: "warehouse_material_inventory",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_warehouse_material_inventory_commodity_owner_location_quality",
                table: "warehouse_material_inventory",
                columns: new[] { "commodity_id", "owner_user_id", "location", "quality" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warehouse_material_inventory");
        }
    }
}
