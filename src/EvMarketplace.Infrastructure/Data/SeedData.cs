using EvMarketplace.Domain.Models;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(EvMarketplaceDbContext context)
    {
        // Check if we already have data
        if (await context.ElectricVehicles.AnyAsync())
        {
            return; // Database is already seeded
        }

        // Create sample EVs using the factory method with validation
        var vehicles = new[]
        {
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Tesla",
                "Model 3 Long Range",
                2024,
                75.0m,
                602.0m,
                14.4m,
                11.0m,
                250.0m,
                Seq("CCS", "Type 2"),
                "Sedan",
                42990.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Tesla",
                "Model Y Performance",
                2024,
                75.0m,
                514.0m,
                17.1m,
                11.0m,
                250.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                54990.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Volkswagen",
                "ID.3 Pro Performance",
                2024,
                77.0m,
                553.0m,
                15.4m,
                11.0m,
                125.0m,
                Seq("CCS", "Type 2"),
                "Hatchback",
                38950.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Volkswagen",
                "ID.4 Pro",
                2024,
                77.0m,
                520.0m,
                16.3m,
                11.0m,
                135.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                42950.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "BMW",
                "i4 eDrive40",
                2024,
                80.7m,
                590.0m,
                15.8m,
                11.0m,
                200.0m,
                Seq("CCS", "Type 2"),
                "Sedan",
                55400.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "BMW",
                "iX xDrive50",
                2024,
                111.5m,
                630.0m,
                19.8m,
                11.0m,
                200.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                85000.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Hyundai",
                "Ioniq 5 Long Range",
                2024,
                72.6m,
                481.0m,
                16.8m,
                11.0m,
                220.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                44995.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Hyundai",
                "Kona Electric",
                2024,
                64.8m,
                484.0m,
                14.7m,
                11.0m,
                102.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                36995.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Kia",
                "EV6 GT-Line",
                2024,
                77.4m,
                528.0m,
                16.5m,
                11.0m,
                240.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                48995.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Nissan",
                "Ariya 87kWh",
                2024,
                87.0m,
                520.0m,
                18.4m,
                22.0m,
                130.0m,
                Seq("CCS", "CHAdeMO", "Type 2"),
                "SUV",
                48995.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "MG",
                "MG4 Long Range",
                2024,
                64.0m,
                450.0m,
                15.7m,
                11.0m,
                144.0m,
                Seq("CCS", "Type 2"),
                "Hatchback",
                31995.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Polestar",
                "Polestar 2 Long Range",
                2024,
                78.0m,
                540.0m,
                16.2m,
                11.0m,
                155.0m,
                Seq("CCS", "Type 2"),
                "Sedan",
                44950.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Mercedes-Benz",
                "EQE 350+",
                2024,
                90.6m,
                660.0m,
                15.7m,
                11.0m,
                170.0m,
                Seq("CCS", "Type 2"),
                "Sedan",
                74500.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Audi",
                "Q4 e-tron 50 quattro",
                2024,
                82.0m,
                528.0m,
                17.4m,
                11.0m,
                135.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                53000.0m
            ),
            ElectricVehicle.Create(
                Guid.NewGuid(),
                "Ford",
                "Mustang Mach-E Extended Range",
                2024,
                91.0m,
                600.0m,
                17.0m,
                11.0m,
                150.0m,
                Seq("CCS", "Type 2"),
                "SUV",
                50830.0m
            )
        };

        // Filter successful validations and add to context
        var validVehicles = vehicles
            .Where(result => result.IsRight)
            .Select(result => result.IfLeft(_ => throw new InvalidOperationException("Should not happen")))
            .ToList();

        await context.ElectricVehicles.AddRangeAsync(validVehicles);
        await context.SaveChangesAsync();
    }
}
