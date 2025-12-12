using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIMS.Data.MigrationsPostgres
{
    /// <inheritdoc />
    public partial class AddSessionSlotToClassSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionSlot",
                table: "ClassSessions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql("UPDATE \"ClassSessions\" SET \"SessionSlot\" = 1 WHERE \"SessionSlot\" = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionSlot",
                table: "ClassSessions");
        }
    }
}
