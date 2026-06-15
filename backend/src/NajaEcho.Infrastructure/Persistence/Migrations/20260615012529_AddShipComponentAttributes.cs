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

            // Read-only projection derived live from sc.item_attributes (pivot by attribute name,
            // one row per item). Size mirrors int.TryParse(value.Trim()): optional sign + digits
            // only; non-numeric/decimal/overflow -> NULL. attributes_fetched_at = MAX(fetched_at).
            migrationBuilder.Sql("""
                CREATE VIEW sc.ship_component_attributes AS
                SELECT
                    ia.item_id AS item_id,
                    max(ia.value) FILTER (WHERE lower(btrim(ia.attribute_name)) = 'class') AS class,
                    CASE
                        WHEN btrim(max(ia.value) FILTER (WHERE lower(btrim(ia.attribute_name)) = 'size')) ~ '^[+-]?\d+$'
                         AND length(regexp_replace(
                               btrim(max(ia.value) FILTER (WHERE lower(btrim(ia.attribute_name)) = 'size')),
                               '[^0-9]', '', 'g')) <= 9
                        THEN btrim(max(ia.value) FILTER (WHERE lower(btrim(ia.attribute_name)) = 'size'))::int
                    END AS size,
                    max(ia.value) FILTER (WHERE lower(btrim(ia.attribute_name)) = 'grade') AS grade,
                    max(ia.fetched_at) AS attributes_fetched_at
                FROM sc.item_attributes ia
                GROUP BY ia.item_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the view first — it depends on sc.item_attributes.
            migrationBuilder.Sql("DROP VIEW IF EXISTS sc.ship_component_attributes;");

            migrationBuilder.DropTable(
                name: "item_attributes",
                schema: "sc");
        }
    }
}
