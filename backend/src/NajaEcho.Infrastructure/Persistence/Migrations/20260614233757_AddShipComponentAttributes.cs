using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShipComponentAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "item_attributes",
                schema: "sc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uex_attribute_id = table.Column<int>(type: "integer", nullable: true),
                    uex_item_id = table.Column<int>(type: "integer", nullable: false),
                    uex_category_id = table.Column<int>(type: "integer", nullable: true),
                    uex_category_attribute_id = table.Column<int>(type: "integer", nullable: false),
                    attribute_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    unit = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    source_date_added = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    source_date_modified = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_item_attributes", x => x.id);
                    table.ForeignKey(
                        name: "fk_item_attributes_item_id",
                        column: x => x.item_id,
                        principalSchema: "sc",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ship_component_attributes",
                schema: "sc",
                columns: table => new
                {
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    @class = table.Column<string>(name: "class", type: "character varying(128)", maxLength: 128, nullable: true),
                    size = table.Column<int>(type: "integer", nullable: true),
                    grade = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    attributes_fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ship_component_attributes", x => x.item_id);
                    table.ForeignKey(
                        name: "fk_ship_component_attributes_item_id",
                        column: x => x.item_id,
                        principalSchema: "sc",
                        principalTable: "items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_item_attributes_item_id",
                schema: "sc",
                table: "item_attributes",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ux_item_attributes_item_category_attr",
                schema: "sc",
                table: "item_attributes",
                columns: new[] { "item_id", "uex_category_attribute_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ship_component_attributes_class",
                schema: "sc",
                table: "ship_component_attributes",
                column: "class");

            migrationBuilder.CreateIndex(
                name: "ix_ship_component_attributes_grade",
                schema: "sc",
                table: "ship_component_attributes",
                column: "grade");

            migrationBuilder.CreateIndex(
                name: "ix_ship_component_attributes_size",
                schema: "sc",
                table: "ship_component_attributes",
                column: "size");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "item_attributes",
                schema: "sc");

            migrationBuilder.DropTable(
                name: "ship_component_attributes",
                schema: "sc");
        }
    }
}
