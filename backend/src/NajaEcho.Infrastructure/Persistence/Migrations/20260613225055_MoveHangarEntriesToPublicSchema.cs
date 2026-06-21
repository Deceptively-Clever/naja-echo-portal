using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NajaEcho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveHangarEntriesToPublicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE sc.hangar_entries SET SCHEMA public;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE public.hangar_entries SET SCHEMA sc;");
        }
    }
}
