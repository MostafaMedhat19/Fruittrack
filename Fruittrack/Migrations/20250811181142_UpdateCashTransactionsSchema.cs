using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCashTransactionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidBackAmount",
                table: "CashReceiptTransactions");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "CashReceiptTransactions");

            migrationBuilder.DropColumn(
                name: "Credit",
                table: "CashDisbursementTransactions");

            migrationBuilder.DropColumn(
                name: "Debit",
                table: "CashDisbursementTransactions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CashDisbursementTransactions");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CashReceiptTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CashReceiptTransactions");

            migrationBuilder.AddColumn<decimal>(
                name: "PaidBackAmount",
                table: "CashReceiptTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAmount",
                table: "CashReceiptTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Credit",
                table: "CashDisbursementTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Debit",
                table: "CashDisbursementTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CashDisbursementTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
