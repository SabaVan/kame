using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Bars_BarId",
                table: "CreditTransactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarId",
                table: "CreditTransactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Bars_BarId",
                table: "CreditTransactions",
                column: "BarId",
                principalTable: "Bars",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditTransactions_Bars_BarId",
                table: "CreditTransactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "BarId",
                table: "CreditTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditTransactions_Bars_BarId",
                table: "CreditTransactions",
                column: "BarId",
                principalTable: "Bars",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
