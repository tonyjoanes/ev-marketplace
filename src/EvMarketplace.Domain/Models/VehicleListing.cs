using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents an actual vehicle listing (new or used) by a dealer or private seller
/// </summary>
public sealed record VehicleListing
{
    public required Guid Id { get; init; }
    public required Guid SellerId { get; init; }

    // Vehicle details (can reference ElectricVehicle or store independently)
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public required decimal BatteryCapacityKwh { get; init; }
    public required decimal WltpRangeKm { get; init; }
    public required decimal EfficiencyKwhPer100Km { get; init; }
    public required string BodyType { get; init; }

    // Listing-specific details
    public required VehicleCondition Condition { get; init; }
    public required decimal Mileage { get; init; }
    public required decimal AskingPriceGbp { get; init; }
    public required string Location { get; init; }
    public required string Description { get; init; }
    public required Seq<string> ImageUrls { get; init; }
    public required ListingStatus Status { get; init; }
    public required DateTime ListedAt { get; init; }
    public required Option<DateTime> SoldAt { get; init; }

    // Optional: Reference to catalogue vehicle for detailed specs
    public required Option<Guid> CatalogueVehicleId { get; init; }

    public static Either<string, VehicleListing> Create(
        Guid id,
        Guid sellerId,
        string make,
        string model,
        int year,
        decimal batteryCapacityKwh,
        decimal wltpRangeKm,
        decimal efficiencyKwhPer100Km,
        string bodyType,
        VehicleCondition condition,
        decimal mileage,
        decimal askingPriceGbp,
        string location,
        string description,
        Seq<string> imageUrls,
        Option<Guid> catalogueVehicleId = default,
        ListingStatus status = ListingStatus.Active,
        DateTime? listedAt = null)
    {
        var validations = Seq(
            ValidateMake(make),
            ValidateModel(model),
            ValidateYear(year),
            ValidateBatteryCapacity(batteryCapacityKwh),
            ValidateRange(wltpRangeKm),
            ValidateEfficiency(efficiencyKwhPer100Km),
            ValidateBodyType(bodyType),
            ValidateMileage(mileage, condition),
            ValidatePrice(askingPriceGbp),
            ValidateLocation(location),
            ValidateDescription(description)
        );

        var errors = validations.Lefts().ToSeq();

        return errors.IsEmpty
            ? new VehicleListing
            {
                Id = id,
                SellerId = sellerId,
                Make = make,
                Model = model,
                Year = year,
                BatteryCapacityKwh = batteryCapacityKwh,
                WltpRangeKm = wltpRangeKm,
                EfficiencyKwhPer100Km = efficiencyKwhPer100Km,
                BodyType = bodyType,
                Condition = condition,
                Mileage = mileage,
                AskingPriceGbp = askingPriceGbp,
                Location = location,
                Description = description,
                ImageUrls = imageUrls,
                CatalogueVehicleId = catalogueVehicleId,
                Status = status,
                ListedAt = listedAt ?? DateTime.UtcNow,
                SoldAt = None
            }
            : string.Join(", ", errors);
    }

    private static Either<string, Unit> ValidateMake(string make) =>
        string.IsNullOrWhiteSpace(make) ? "Make cannot be empty" : unit;

    private static Either<string, Unit> ValidateModel(string model) =>
        string.IsNullOrWhiteSpace(model) ? "Model cannot be empty" : unit;

    private static Either<string, Unit> ValidateYear(int year) =>
        year < 2010 || year > DateTime.UtcNow.Year + 1
            ? "Year must be between 2010 and next year"
            : unit;

    private static Either<string, Unit> ValidateBatteryCapacity(decimal capacity) =>
        capacity <= 0 ? "Battery capacity must be positive" : unit;

    private static Either<string, Unit> ValidateRange(decimal range) =>
        range <= 0 ? "Range must be positive" : unit;

    private static Either<string, Unit> ValidateEfficiency(decimal efficiency) =>
        efficiency <= 0 ? "Efficiency must be positive" : unit;

    private static Either<string, Unit> ValidateBodyType(string bodyType) =>
        string.IsNullOrWhiteSpace(bodyType) ? "Body type cannot be empty" : unit;

    private static Either<string, Unit> ValidateMileage(
        decimal mileage,
        VehicleCondition condition) =>
        mileage < 0
            ? "Mileage cannot be negative"
            : condition == VehicleCondition.New && mileage > 100
            ? "New vehicles should have minimal mileage"
            : unit;

    private static Either<string, Unit> ValidatePrice(decimal price) =>
        price <= 0 ? "Price must be positive" : unit;

    private static Either<string, Unit> ValidateLocation(string location) =>
        string.IsNullOrWhiteSpace(location) ? "Location is required" : unit;

    private static Either<string, Unit> ValidateDescription(string description) =>
        string.IsNullOrWhiteSpace(description) || description.Length < 20
            ? "Description must be at least 20 characters"
            : unit;
}

public enum VehicleCondition
{
    New,
    Excellent,
    Good,
    Fair
}

public enum ListingStatus
{
    Active,
    Sold,
    Expired,
    Removed
}
