using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class AddCashReceiptTransactionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyEntries_Factories_FactoryId",
                table: "SupplyEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplyEntries_Farms_FarmId",
                table: "SupplyEntries");

            migrationBuilder.CreateTable(
                name: "CashReceiptTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidBackAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m),
                    RemainingAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, defaultValue: 0m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashReceiptTransactions", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyEntries_Factories_FactoryId",
                table: "SupplyEntries",
                column: "FactoryId",
                principalTable: "Factories",
                principalColumn: "FactoryId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyEntries_Farms_FarmId",
                table: "SupplyEntries",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "FarmId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyEntries_Factories_FactoryId",
                table: "SupplyEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplyEntries_Farms_FarmId",
                table: "SupplyEntries");

            migrationBuilder.DropTable(
                name: "CashReceiptTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyEntries_Factories_FactoryId",
                table: "SupplyEntries",
                column: "FactoryId",
                principalTable: "Factories",
                principalColumn: "FactoryId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyEntries_Farms_FarmId",
                table: "SupplyEntries",
                column: "FarmId",
                principalTable: "Farms",
                principalColumn: "FarmId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
