using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EvMarketplace.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSellersAndListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create Sellers table
            migrationBuilder.CreateTable(
                name: "Sellers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sellers", x => x.Id);
                });

            // Create VehicleListings table
            migrationBuilder.CreateTable(
                name: "VehicleListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    BatteryCapacityKwh = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    WltpRangeKm = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    EfficiencyKwhPer100Km = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BodyType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Condition = table.Column<int>(type: "integer", nullable: false),
                    Mileage = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AskingPriceGbp = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ImageUrls = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ListedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CatalogueVehicleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VehicleListings_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleListings_ElectricVehicles_CatalogueVehicleId",
                        column: x => x.CatalogueVehicleId,
                        principalTable: "ElectricVehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Sellers_Email",
                table: "Sellers",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_Type",
                table: "Sellers",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_SellerId",
                table: "VehicleListings",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Make",
                table: "VehicleListings",
                column: "Make");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Status",
                table: "VehicleListings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_Condition",
                table: "VehicleListings",
                column: "Condition");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_AskingPriceGbp",
                table: "VehicleListings",
                column: "AskingPriceGbp");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_ListedAt",
                table: "VehicleListings",
                column: "ListedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleListings_CatalogueVehicleId",
                table: "VehicleListings",
                column: "CatalogueVehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "VehicleListings");
            migrationBuilder.DropTable(name: "Sellers");
        }
    }
}
