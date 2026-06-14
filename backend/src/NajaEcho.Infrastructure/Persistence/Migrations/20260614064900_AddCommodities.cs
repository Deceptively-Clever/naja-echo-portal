using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommodities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "commodities",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    uuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    kind = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    weight_scu = table.Column<int>(type: "integer", nullable: true),
                    id_parent = table.Column<int>(type: "integer", nullable: true),
                    id_item = table.Column<int>(type: "integer", nullable: true),
                    wiki = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ids_star_systems_raw = table.Column<string>(type: "text", nullable: true),
                    ids_planets_raw = table.Column<string>(type: "text", nullable: true),
                    ids_moons_raw = table.Column<string>(type: "text", nullable: true),
                    ids_poi_raw = table.Column<string>(type: "text", nullable: true),
                    ids_orbits_raw = table.Column<string>(type: "text", nullable: true),
                    ids_star_systems = table.Column<int[]>(type: "integer[]", nullable: false),
                    ids_planets = table.Column<int[]>(type: "integer[]", nullable: false),
                    ids_moons = table.Column<int[]>(type: "integer[]", nullable: false),
                    ids_poi = table.Column<int[]>(type: "integer[]", nullable: false),
                    ids_orbits = table.Column<int[]>(type: "integer[]", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
                    is_available_live = table.Column<bool>(type: "boolean", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_extractable = table.Column<bool>(type: "boolean", nullable: false),
                    is_mineral = table.Column<bool>(type: "boolean", nullable: false),
                    is_raw = table.Column<bool>(type: "boolean", nullable: false),
                    is_pure = table.Column<bool>(type: "boolean", nullable: false),
                    is_refined = table.Column<bool>(type: "boolean", nullable: false),
                    is_refinable = table.Column<bool>(type: "boolean", nullable: false),
                    is_harvestable = table.Column<bool>(type: "boolean", nullable: false),
                    is_buyable = table.Column<bool>(type: "boolean", nullable: false),
                    is_sellable = table.Column<bool>(type: "boolean", nullable: false),
                    is_temporary = table.Column<bool>(type: "boolean", nullable: false),
                    is_illegal = table.Column<bool>(type: "boolean", nullable: false),
                    is_volatile_qt = table.Column<bool>(type: "boolean", nullable: false),
                    is_volatile_time = table.Column<bool>(type: "boolean", nullable: false),
                    is_inert = table.Column<bool>(type: "boolean", nullable: false),
                    is_explosive = table.Column<bool>(type: "boolean", nullable: false),
                    is_buggy = table.Column<bool>(type: "boolean", nullable: false),
                    is_fuel = table.Column<bool>(type: "boolean", nullable: false),
                    source_date_added = table.Column<long>(type: "bigint", nullable: true),
                    source_date_modified = table.Column<long>(type: "bigint", nullable: true),
                    source_date_added_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_date_modified_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_commodities", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_commodities_status",
                schema: "sc",
                table: "commodities",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_commodities_uex_id",
                schema: "sc",
                table: "commodities",
                column: "uex_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commodities",
                schema: "sc");
        }
    }
}
