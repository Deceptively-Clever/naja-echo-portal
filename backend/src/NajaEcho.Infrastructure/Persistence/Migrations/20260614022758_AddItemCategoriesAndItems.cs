using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemCategoriesAndItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_categories",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    section = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_game_related = table.Column<bool>(type: "boolean", nullable: false),
                    is_mining = table.Column<bool>(type: "boolean", nullable: false),
                    source_date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_date_modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "items",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    uuid = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    uex_id = table.Column<int>(type: "integer", nullable: false),
                    id_parent = table.Column<int>(type: "integer", nullable: true),
                    id_category = table.Column<int>(type: "integer", nullable: false),
                    id_company = table.Column<int>(type: "integer", nullable: true),
                    id_vehicle = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    section = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    category = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    company_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    vehicle_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    slug = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    size = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    color = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    color2 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    url_store = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    wiki = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    quality = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_exclusive_pledge = table.Column<bool>(type: "boolean", nullable: false),
                    is_exclusive_subscriber = table.Column<bool>(type: "boolean", nullable: false),
                    is_exclusive_concierge = table.Column<bool>(type: "boolean", nullable: false),
                    is_commodity = table.Column<bool>(type: "boolean", nullable: false),
                    is_harvestable = table.Column<bool>(type: "boolean", nullable: false),
                    notification = table.Column<string>(type: "text", nullable: true),
                    game_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    source_date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_date_modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    raw_data = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    soft_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_categories_section",
                schema: "sc",
                table: "item_categories",
                column: "section");

            migrationBuilder.CreateIndex(
                name: "ix_item_categories_type",
                schema: "sc",
                table: "item_categories",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_item_categories_uex_id",
                schema: "sc",
                table: "item_categories",
                column: "uex_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_items_id_category",
                schema: "sc",
                table: "items",
                column: "id_category");

            migrationBuilder.CreateIndex(
                name: "ix_items_status",
                schema: "sc",
                table: "items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_items_uex_id",
                schema: "sc",
                table: "items",
                column: "uex_id");

            migrationBuilder.CreateIndex(
                name: "ix_items_uuid",
                schema: "sc",
                table: "items",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_categories",
                schema: "sc");

            migrationBuilder.DropTable(
                name: "items",
                schema: "sc");
        }
    }
}
