using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Calculators;

/// <summary>
/// Pure functions for EV-related calculations.
/// All functions are static, pure (no side effects), and composable.
/// </summary>
public static class EvCalculators
{
    /// <summary>
    /// Calculate monthly running cost based on annual mileage and electricity cost
    /// </summary>
    public static Either<string, decimal> CalculateMonthlyRunningCost(
        decimal efficiencyKwhPer100Km,
        decimal annualMileageKm,
        decimal electricityCostPencePerKwh) =>
        from _ in ValidateEfficiency(efficiencyKwhPer100Km)
        from __ in ValidateMileage(annualMileageKm)
        from ___ in ValidateElectricityCost(electricityCostPencePerKwh)
        let annualEnergyKwh = (annualMileageKm / 100) * efficiencyKwhPer100Km
        let annualCostPence = annualEnergyKwh * electricityCostPencePerKwh
        let monthlyCostGbp = (annualCostPence / 12) / 100
        select Math.Round(monthlyCostGbp, 2);

    /// <summary>
    /// Calculate charging time from 10% to 80% based on battery size and charger power
    /// Uses a simplified model: usable capacity * 0.7 (70% charge) / charger power
    /// </summary>
    public static Either<string, decimal> CalculateChargingTime10To80(
        decimal batteryCapacityKwh,
        decimal chargerPowerKw) =>
        from _ in ValidateBatteryCapacity(batteryCapacityKwh)
        from __ in ValidateChargerPower(chargerPowerKw)
        let usableCapacity = batteryCapacityKwh * 0.9m
        let chargeAmount = usableCapacity * 0.7m
        let chargingTimeHours = chargeAmount / chargerPowerKw
        select Math.Round(chargingTimeHours, 2);

    /// <summary>
    /// Calculate simple Total Cost of Ownership over N years
    /// Includes purchase price and energy costs only (simplified model)
    /// </summary>
    public static Either<string, TcoResult> CalculateSimpleTco(
        decimal purchasePriceGbp,
        decimal efficiencyKwhPer100Km,
        decimal annualMileageKm,
        decimal electricityCostPencePerKwh,
        int years) =>
        from _ in ValidatePrice(purchasePriceGbp)
        from monthlyRunningCost in CalculateMonthlyRunningCost(
            efficiencyKwhPer100Km,
            annualMileageKm,
            electricityCostPencePerKwh)
        from __ in ValidateYears(years)
        let totalEnergyCost = monthlyRunningCost * 12 * years
        let totalCost = purchasePriceGbp + totalEnergyCost
        select new TcoResult(
            PurchasePriceGbp: purchasePriceGbp,
            TotalEnergyCostGbp: totalEnergyCost,
            TotalCostGbp: Math.Round(totalCost, 2),
            Years: years,
            AnnualMileageKm: annualMileageKm
        );

    /// <summary>
    /// Calculate range anxiety factor based on WLTP range and typical daily commute
    /// Returns a percentage indicating how much buffer exists (higher is better)
    /// </summary>
    public static Either<string, decimal> CalculateRangeAnxietyFactor(
        decimal wltpRangeKm,
        decimal dailyCommuteKm) =>
        from _ in ValidateRange(wltpRangeKm)
        from __ in ValidateCommute(dailyCommuteKm)
        let weeklyCommute = dailyCommuteKm * 5
        let bufferPercentage = ((wltpRangeKm - weeklyCommute) / wltpRangeKm) * 100
        select Math.Round(bufferPercentage, 1);

    // Validation functions
    private static Either<string, Unit> ValidateEfficiency(decimal efficiency) =>
        efficiency > 0
            ? unit
            : "Efficiency must be positive";

    private static Either<string, Unit> ValidateMileage(decimal mileage) =>
        mileage >= 0
            ? unit
            : "Mileage cannot be negative";

    private static Either<string, Unit> ValidateElectricityCost(decimal cost) =>
        cost >= 0
            ? unit
            : "Electricity cost cannot be negative";

    private static Either<string, Unit> ValidateBatteryCapacity(decimal capacity) =>
        capacity > 0
            ? unit
            : "Battery capacity must be positive";

    private static Either<string, Unit> ValidateChargerPower(decimal power) =>
        power > 0
            ? unit
            : "Charger power must be positive";

    private static Either<string, Unit> ValidatePrice(decimal price) =>
        price >= 0
            ? unit
            : "Price cannot be negative";

    private static Either<string, Unit> ValidateYears(int years) =>
        years > 0 && years <= 20
            ? unit
            : "Years must be between 1 and 20";

    private static Either<string, Unit> ValidateRange(decimal range) =>
        range > 0
            ? unit
            : "Range must be positive";

    private static Either<string, Unit> ValidateCommute(decimal commute) =>
        commute >= 0
            ? unit
            : "Commute distance cannot be negative";
}

/// <summary>
/// Result of Total Cost of Ownership calculation
/// </summary>
public sealed record TcoResult(
    decimal PurchasePriceGbp,
    decimal TotalEnergyCostGbp,
    decimal TotalCostGbp,
    int Years,
    decimal AnnualMileageKm
);
