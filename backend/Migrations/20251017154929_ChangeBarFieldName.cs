using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBarFieldName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "_openAtUtc",
                table: "Bars",
                newName: "OpenAtUtc");

            migrationBuilder.RenameColumn(
                name: "_closeAtUtc",
                table: "Bars",
                newName: "CloseAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenAtUtc",
                table: "Bars",
                newName: "_openAtUtc");

            migrationBuilder.RenameColumn(
                name: "CloseAtUtc",
                table: "Bars",
                newName: "_closeAtUtc");
        }
    }
}
