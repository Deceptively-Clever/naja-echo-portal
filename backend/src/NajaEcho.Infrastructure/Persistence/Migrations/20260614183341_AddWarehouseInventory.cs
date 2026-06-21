using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWarehouseInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "warehouse_inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_inventory", x => x.id);
                    table.CheckConstraint("ck_warehouse_inventory_quantity", "quantity >= 1");
                    table.ForeignKey(
                        name: "fk_warehouse_inventory_item_id",
                        column: x => x.item_id,
                        principalSchema: "sc",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_inventory_item_id",
                table: "warehouse_inventory",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_inventory_owner_user_id",
                table: "warehouse_inventory",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_warehouse_inventory_item_owner_location",
                table: "warehouse_inventory",
                columns: new[] { "item_id", "owner_user_id", "location" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warehouse_inventory");
        }
    }
}
