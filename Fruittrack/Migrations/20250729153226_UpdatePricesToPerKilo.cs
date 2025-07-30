using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fruittrack.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePricesToPerKilo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FarmPricePerTon",
                table: "SupplyEntries",
                newName: "FarmPricePerKilo");

            migrationBuilder.RenameColumn(
                name: "FactoryPricePerTon",
                table: "SupplyEntries",
                newName: "FactoryPricePerKilo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FarmPricePerKilo",
                table: "SupplyEntries",
                newName: "FarmPricePerTon");

            migrationBuilder.RenameColumn(
                name: "FactoryPricePerKilo",
                table: "SupplyEntries",
                newName: "FactoryPricePerTon");
        }
    }
}
