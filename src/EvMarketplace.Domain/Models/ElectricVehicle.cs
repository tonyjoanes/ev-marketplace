using LanguageExt;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Immutable record representing an Electric Vehicle in the catalogue.
/// Using records for immutability and value semantics.
/// </summary>
public sealed record ElectricVehicle
{
    public required Guid Id { get; init; }
    public required string Make { get; init; }
    public required string Model { get; init; }
    public required int Year { get; init; }
    public required decimal BatteryCapacityKwh { get; init; }
    public required decimal WltpRangeKm { get; init; }
    public required decimal EfficiencyKwhPer100Km { get; init; }
    public required decimal AcChargeRateKw { get; init; }
    public required decimal DcChargeRateKw { get; init; }
    public required Seq<string> ConnectorTypes { get; init; }
    public required string BodyType { get; init; }
    public required decimal PriceGbp { get; init; }

    /// <summary>
    /// Factory method to create an EV with validation
    /// </summary>
    public static Either<string, ElectricVehicle> Create(
        Guid id,
        string make,
        string model,
        int year,
        decimal batteryCapacityKwh,
        decimal wltpRangeKm,
        decimal efficiencyKwhPer100Km,
        decimal acChargeRateKw,
        decimal dcChargeRateKw,
        Seq<string> connectorTypes,
        string bodyType,
        decimal priceGbp)
    {
        var validations = Seq(
            ValidateMake(make),
            ValidateModel(model),
            ValidateYear(year),
            ValidateBatteryCapacity(batteryCapacityKwh),
            ValidateRange(wltpRangeKm),
            ValidateEfficiency(efficiencyKwhPer100Km),
            ValidateChargeRate(acChargeRateKw, "AC"),
            ValidateChargeRate(dcChargeRateKw, "DC"),
            ValidateConnectorTypes(connectorTypes),
            ValidateBodyType(bodyType),
            ValidatePrice(priceGbp)
        );

        var errors = validations
            .Lefts()
            .ToSeq();

        return errors.IsEmpty
            ? new ElectricVehicle
            {
                Id = id,
                Make = make,
                Model = model,
                Year = year,
                BatteryCapacityKwh = batteryCapacityKwh,
                WltpRangeKm = wltpRangeKm,
                EfficiencyKwhPer100Km = efficiencyKwhPer100Km,
                AcChargeRateKw = acChargeRateKw,
                DcChargeRateKw = dcChargeRateKw,
                ConnectorTypes = connectorTypes,
                BodyType = bodyType,
                PriceGbp = priceGbp
            }
            : string.Join(", ", errors);
    }

    private static Either<string, Unit> ValidateMake(string make) =>
        string.IsNullOrWhiteSpace(make)
            ? "Make cannot be empty"
            : unit;

    private static Either<string, Unit> ValidateModel(string model) =>
        string.IsNullOrWhiteSpace(model)
            ? "Model cannot be empty"
            : unit;

    private static Either<string, Unit> ValidateYear(int year) =>
        year < 2010 || year > DateTime.UtcNow.Year + 2
            ? "Year must be between 2010 and 2 years in the future"
            : unit;

    private static Either<string, Unit> ValidateBatteryCapacity(decimal capacity) =>
        capacity <= 0
            ? "Battery capacity must be positive"
            : unit;

    private static Either<string, Unit> ValidateRange(decimal range) =>
        range <= 0
            ? "Range must be positive"
            : unit;

    private static Either<string, Unit> ValidateEfficiency(decimal efficiency) =>
        efficiency <= 0
            ? "Efficiency must be positive"
            : unit;

    private static Either<string, Unit> ValidateChargeRate(decimal rate, string type) =>
        rate <= 0
            ? $"{type} charge rate must be positive"
            : unit;

    private static Either<string, Unit> ValidateConnectorTypes(Seq<string> connectors) =>
        connectors.IsEmpty
            ? "At least one connector type must be specified"
            : unit;

    private static Either<string, Unit> ValidateBodyType(string bodyType) =>
        string.IsNullOrWhiteSpace(bodyType)
            ? "Body type cannot be empty"
            : unit;

    private static Either<string, Unit> ValidatePrice(decimal price) =>
        price <= 0
            ? "Price must be positive"
            : unit;
}
