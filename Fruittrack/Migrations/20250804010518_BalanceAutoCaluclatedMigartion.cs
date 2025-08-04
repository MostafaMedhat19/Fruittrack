using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class BalanceAutoCaluclatedMigartion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Balance",
                table: "CashDisbursementTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "CashDisbursementTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
