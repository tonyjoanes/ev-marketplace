using EvMarketplace.Domain.Calculators;
using FluentAssertions;
using LanguageExt;

namespace EvMarketplace.Tests.Calculators;

public class EvCalculatorsTests
{
    [Theory]
    [InlineData(15, 10000, 30, 37.50)]
    [InlineData(20, 15000, 25, 62.50)]
    [InlineData(18, 12000, 28, 50.40)]
    public void CalculateMonthlyRunningCost_ValidInputs_ReturnsCorrectCost(
        decimal efficiencyKwhPer100Km,
        decimal annualMileageKm,
        decimal electricityCostPencePerKwh,
        decimal expectedMonthlyCostGbp)
    {
        // Act
        var result = EvCalculators.CalculateMonthlyRunningCost(
            efficiencyKwhPer100Km,
            annualMileageKm,
            electricityCostPencePerKwh);

        // Assert - functional style with pattern matching
        result.Match(
            Right: cost => cost.Should().Be(expectedMonthlyCostGbp),
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void CalculateMonthlyRunningCost_NegativeEfficiency_ReturnsError()
    {
        // Act
        var result = EvCalculators.CalculateMonthlyRunningCost(-15, 10000, 30);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Efficiency"));
    }

    [Theory]
    [InlineData(75, 50, 1.05)]
    [InlineData(60, 150, 0.28)]
    [InlineData(100, 250, 0.25)]
    public void CalculateChargingTime10To80_ValidInputs_ReturnsCorrectTime(
        decimal batteryCapacityKwh,
        decimal chargerPowerKw,
        decimal expectedTimeHours)
    {
        // Act
        var result = EvCalculators.CalculateChargingTime10To80(
            batteryCapacityKwh,
            chargerPowerKw);

        // Assert
        result.Match(
            Right: time => time.Should().Be(expectedTimeHours),
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void CalculateChargingTime10To80_ZeroChargerPower_ReturnsError()
    {
        // Act
        var result = EvCalculators.CalculateChargingTime10To80(75, 0);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public void CalculateSimpleTco_ValidInputs_ReturnsCorrectTco()
    {
        // Arrange
        const decimal purchasePrice = 35000;
        const decimal efficiency = 18;
        const decimal annualMileage = 12000;
        const decimal electricityCost = 28;
        const int years = 5;

        // Act
        var result = EvCalculators.CalculateSimpleTco(
            purchasePrice,
            efficiency,
            annualMileage,
            electricityCost,
            years);

        // Assert
        result.Match(
            Right: tco =>
            {
                tco.PurchasePriceGbp.Should().Be(purchasePrice);
                tco.Years.Should().Be(years);
                tco.TotalCostGbp.Should().BeGreaterThan(purchasePrice);
                tco.TotalEnergyCostGbp.Should().BeGreaterThan(0);
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(21)]
    public void CalculateSimpleTco_InvalidYears_ReturnsError(int invalidYears)
    {
        // Act
        var result = EvCalculators.CalculateSimpleTco(35000, 18, 12000, 28, invalidYears);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Theory]
    [InlineData(400, 50, 75)]
    [InlineData(300, 100, 33.3)]
    [InlineData(500, 80, 68)]
    public void CalculateRangeAnxietyFactor_ValidInputs_ReturnsCorrectFactor(
        decimal wltpRangeKm,
        decimal dailyCommuteKm,
        decimal expectedBufferPercentage)
    {
        // Act
        var result = EvCalculators.CalculateRangeAnxietyFactor(wltpRangeKm, dailyCommuteKm);

        // Assert
        result.Match(
            Right: factor => factor.Should().BeApproximately(expectedBufferPercentage, 0.1m),
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void CalculateRangeAnxietyFactor_NegativeCommute_ReturnsError()
    {
        // Act
        var result = EvCalculators.CalculateRangeAnxietyFactor(400, -10);

        // Assert
        result.IsLeft.Should().BeTrue();
    }
}
