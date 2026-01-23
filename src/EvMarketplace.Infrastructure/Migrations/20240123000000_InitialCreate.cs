using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvMarketplace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ElectricVehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    BatteryCapacityKwh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    WltpRangeKm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EfficiencyKwhPer100Km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AcChargeRateKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DcChargeRateKw = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ConnectorTypes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BodyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PriceGbp = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricVehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectricVehicles_BodyType",
                table: "ElectricVehicles",
                column: "BodyType");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricVehicles_Make",
                table: "ElectricVehicles",
                column: "Make");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricVehicles_PriceGbp",
                table: "ElectricVehicles",
                column: "PriceGbp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectricVehicles");
        }
    }
}
