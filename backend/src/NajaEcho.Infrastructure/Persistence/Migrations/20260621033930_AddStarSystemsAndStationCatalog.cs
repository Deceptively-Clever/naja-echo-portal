using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStarSystemsAndStationCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "warehouse_material_inventory",
                newName: "warehouse_material_inventory",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "warehouse_inventory",
                newName: "warehouse_inventory",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "pending_character_registrations",
                newName: "pending_character_registrations",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "hangar_entries",
                newName: "hangar_entries",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "characters",
                newName: "characters",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "public");

            migrationBuilder.AddColumn<Guid>(
                name: "station_id",
                schema: "public",
                table: "warehouse_material_inventory",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "station_id",
                schema: "public",
                table: "warehouse_inventory",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "star_systems",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_star_systems", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "space_stations",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    star_system_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    nickname = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_decommissioned = table.Column<bool>(type: "boolean", nullable: false),
                    is_landable = table.Column<bool>(type: "boolean", nullable: false),
                    has_refinery = table.Column<bool>(type: "boolean", nullable: false),
                    has_trade_terminal = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_space_stations", x => x.id);
                    table.ForeignKey(
                        name: "fk_space_stations_star_systems_star_system_id",
                        column: x => x.star_system_id,
                        principalSchema: "sc",
                        principalTable: "star_systems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_material_inventory_station_id",
                schema: "public",
                table: "warehouse_material_inventory",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_inventory_station_id",
                schema: "public",
                table: "warehouse_inventory",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "ix_space_stations_avail_decomm_name",
                schema: "sc",
                table: "space_stations",
                columns: new[] { "is_available", "is_decommissioned", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_space_stations_star_system_id",
                schema: "sc",
                table: "space_stations",
                column: "star_system_id");

            migrationBuilder.CreateIndex(
                name: "ix_space_stations_status",
                schema: "sc",
                table: "space_stations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_space_stations_uex_id",
                schema: "sc",
                table: "space_stations",
                column: "uex_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_star_systems_status",
                schema: "sc",
                table: "star_systems",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_star_systems_uex_id",
                schema: "sc",
                table: "star_systems",
                column: "uex_id",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_warehouse_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.DropForeignKey(
                name: "fk_warehouse_material_inventory_space_stations_station_id",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.DropTable(
                name: "space_stations",
                schema: "sc");

            migrationBuilder.DropTable(
                name: "star_systems",
                schema: "sc");

            migrationBuilder.DropIndex(
                name: "ix_warehouse_material_inventory_station_id",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.DropIndex(
                name: "ix_warehouse_inventory_station_id",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.DropColumn(
                name: "station_id",
                schema: "public",
                table: "warehouse_material_inventory");

            migrationBuilder.DropColumn(
                name: "station_id",
                schema: "public",
                table: "warehouse_inventory");

            migrationBuilder.RenameTable(
                name: "warehouse_material_inventory",
                schema: "public",
                newName: "warehouse_material_inventory");

            migrationBuilder.RenameTable(
                name: "warehouse_inventory",
                schema: "public",
                newName: "warehouse_inventory");

            migrationBuilder.RenameTable(
                name: "pending_character_registrations",
                schema: "public",
                newName: "pending_character_registrations");

            migrationBuilder.RenameTable(
                name: "hangar_entries",
                schema: "public",
                newName: "hangar_entries");

            migrationBuilder.RenameTable(
                name: "characters",
                schema: "public",
                newName: "characters");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "public",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "public",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "public",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "public",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "public",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "public",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "public",
                newName: "AspNetRoleClaims");
        }
    }
}
