using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Factories",
                columns: table => new
                {
                    FactoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factories", x => x.FactoryId);
                });

            migrationBuilder.CreateTable(
                name: "Farms",
                columns: table => new
                {
                    FarmId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FarmName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Farms", x => x.FarmId);
                });

            migrationBuilder.CreateTable(
                name: "Trucks",
                columns: table => new
                {
                    TruckId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TruckNumber = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trucks", x => x.TruckId);
                });

            migrationBuilder.CreateTable(
                name: "SupplyEntries",
                columns: table => new
                {
                    SupplyEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TruckId = table.Column<int>(type: "int", nullable: false),
                    FarmId = table.Column<int>(type: "int", nullable: false),
                    FarmWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FarmDiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FarmPricePerTon = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false),
                    FactoryWeight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FactoryDiscountRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FactoryPricePerTon = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FreightCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TransferFrom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransferTo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplyEntries", x => x.SupplyEntryId);
                    table.ForeignKey(
                        name: "FK_SupplyEntries_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "FactoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyEntries_Farms_FarmId",
                        column: x => x.FarmId,
                        principalTable: "Farms",
                        principalColumn: "FarmId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplyEntries_Trucks_TruckId",
                        column: x => x.TruckId,
                        principalTable: "Trucks",
                        principalColumn: "TruckId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialSettlements",
                columns: table => new
                {
                    SettlementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplyEntryId = table.Column<int>(type: "int", nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReceivedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialSettlements", x => x.SettlementId);
                    table.ForeignKey(
                        name: "FK_FinancialSettlements_SupplyEntries_SupplyEntryId",
                        column: x => x.SupplyEntryId,
                        principalTable: "SupplyEntries",
                        principalColumn: "SupplyEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialSettlements_SupplyEntryId",
                table: "FinancialSettlements",
                column: "SupplyEntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplyEntries_FactoryId",
                table: "SupplyEntries",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyEntries_FarmId",
                table: "SupplyEntries",
                column: "FarmId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplyEntries_TruckId",
                table: "SupplyEntries",
                column: "TruckId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialSettlements");

            migrationBuilder.DropTable(
                name: "SupplyEntries");

            migrationBuilder.DropTable(
                name: "Factories");

            migrationBuilder.DropTable(
                name: "Farms");

            migrationBuilder.DropTable(
                name: "Trucks");
        }
    }
}
