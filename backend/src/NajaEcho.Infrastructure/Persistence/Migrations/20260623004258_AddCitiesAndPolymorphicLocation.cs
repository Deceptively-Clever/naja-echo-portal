using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCitiesAndPolymorphicLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_warehouse_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.DropForeignKey(
                name: "fk_warehouse_material_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.RenameColumn(
                name: "station_id",
                schema: "public",
                table: "warehouse_material_inventory",
                newName: "location_id");

            migrationBuilder.RenameIndex(
                name: "ix_warehouse_material_inventory_station_id",
                schema: "public",
                table: "warehouse_material_inventory",
                newName: "ix_warehouse_material_inventory_location_id");

            migrationBuilder.RenameColumn(
                name: "station_id",
                schema: "public",
                table: "warehouse_inventory",
                newName: "location_id");

            migrationBuilder.RenameIndex(
                name: "ix_warehouse_inventory_station_id",
                schema: "public",
                table: "warehouse_inventory",
                newName: "ix_warehouse_inventory_location_id");

            migrationBuilder.AddColumn<string>(
                name: "location_type",
                schema: "public",
                table: "warehouse_material_inventory",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location_type",
                schema: "public",
                table: "warehouse_inventory",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    star_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_available_live = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_star_systems_star_system_id",
                        column: x => x.star_system_id,
                        principalSchema: "sc",
                        principalTable: "star_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_warehouse_material_inventory_location_type",
                schema: "public",
                table: "warehouse_material_inventory",
                sql: "location_type IN ('Station', 'City')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_warehouse_inventory_location_type",
                schema: "public",
                table: "warehouse_inventory",
                sql: "location_type IN ('Station', 'City')");

            migrationBuilder.CreateIndex(
                name: "ix_cities_avail_visible_name",
                schema: "sc",
                table: "cities",
                columns: new[] { "is_available", "is_visible", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_star_system_id",
                schema: "sc",
                table: "cities",
                column: "star_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_status",
                schema: "sc",
                table: "cities",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_cities_uex_id",
                schema: "sc",
                table: "cities",
                column: "uex_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cities",
                schema: "sc");

            migrationBuilder.DropCheckConstraint(
                name: "ck_warehouse_material_inventory_location_type",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.DropCheckConstraint(
                name: "ck_warehouse_inventory_location_type",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.DropColumn(
                name: "location_type",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.DropColumn(
                name: "location_type",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.RenameColumn(
                name: "location_id",
                schema: "public",
                table: "warehouse_material_inventory",
                newName: "station_id");

            migrationBuilder.RenameIndex(
                name: "ix_warehouse_material_inventory_location_id",
                schema: "public",
                table: "warehouse_material_inventory",
                newName: "ix_warehouse_material_inventory_station_id");

            migrationBuilder.RenameColumn(
                name: "location_id",
                schema: "public",
                table: "warehouse_inventory",
                newName: "station_id");

            migrationBuilder.RenameIndex(
                name: "ix_warehouse_inventory_location_id",
                schema: "public",
                table: "warehouse_inventory",
                newName: "ix_warehouse_inventory_station_id");

            migrationBuilder.AddForeignKey(
                name: "fk_warehouse_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_inventory",
                column: "station_id",
                principalSchema: "sc",
                principalTable: "space_stations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_warehouse_material_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_material_inventory",
                column: "station_id",
                principalSchema: "sc",
                principalTable: "space_stations",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
