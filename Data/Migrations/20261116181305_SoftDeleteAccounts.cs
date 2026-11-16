using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjetoBtg.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_TaxId",
                table: "Accounts");

            migrationBuilder.AddColumn<int>(
                name: "Desativado",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TaxId",
                table: "Accounts",
                column: "TaxId",
                unique: true,
                filter: "\"Desativado\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_TaxId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Desativado",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TaxId",
                table: "Accounts",
                column: "TaxId",
                unique: true);
        }
    }
}
