using EvMarketplace.Domain.Models;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Tests.Models;

public class ElectricVehicleTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsElectricVehicle()
    {
        // Arrange
        var id = Guid.NewGuid();
        var connectors = Seq("CCS", "Type 2");

        // Act
        var result = ElectricVehicle.Create(
            id,
            "Tesla",
            "Model 3",
            2024,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            connectors,
            "Sedan",
            40000.0m);

        // Assert - functional style
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: ev =>
            {
                ev.Make.Should().Be("Tesla");
                ev.Model.Should().Be("Model 3");
                ev.Year.Should().Be(2024);
                ev.BatteryCapacityKwh.Should().Be(75.0m);
                ev.ConnectorTypes.Should().BeEquivalentTo(connectors);
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Theory]
    [InlineData("", "Model 3", "Make cannot be empty")]
    [InlineData("Tesla", "", "Model cannot be empty")]
    [InlineData(null, "Model 3", "Make cannot be empty")]
    public void Create_InvalidMakeOrModel_ReturnsError(
        string? make,
        string? model,
        string expectedErrorSubstring)
    {
        // Act
        var result = ElectricVehicle.Create(
            Guid.NewGuid(),
            make ?? "",
            model ?? "",
            2024,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq("CCS"),
            "Sedan",
            40000.0m);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain(expectedErrorSubstring));
    }

    [Theory]
    [InlineData(2009)]
    [InlineData(2030)]
    public void Create_InvalidYear_ReturnsError(int invalidYear)
    {
        // Act
        var result = ElectricVehicle.Create(
            Guid.NewGuid(),
            "Tesla",
            "Model 3",
            invalidYear,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq("CCS"),
            "Sedan",
            40000.0m);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Year"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_InvalidBatteryCapacity_ReturnsError(decimal invalidCapacity)
    {
        // Act
        var result = ElectricVehicle.Create(
            Guid.NewGuid(),
            "Tesla",
            "Model 3",
            2024,
            invalidCapacity,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq("CCS"),
            "Sedan",
            40000.0m);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Battery capacity"));
    }

    [Fact]
    public void Create_EmptyConnectorTypes_ReturnsError()
    {
        // Act
        var result = ElectricVehicle.Create(
            Guid.NewGuid(),
            "Tesla",
            "Model 3",
            2024,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq<string>(),
            "Sedan",
            40000.0m);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("connector"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000)]
    public void Create_InvalidPrice_ReturnsError(decimal invalidPrice)
    {
        // Act
        var result = ElectricVehicle.Create(
            Guid.NewGuid(),
            "Tesla",
            "Model 3",
            2024,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq("CCS"),
            "Sedan",
            invalidPrice);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Price"));
    }

    [Fact]
    public void ElectricVehicle_IsImmutable()
    {
        // Arrange
        var ev = ElectricVehicle.Create(
            Guid.NewGuid(),
            "Tesla",
            "Model 3",
            2024,
            75.0m,
            500.0m,
            15.0m,
            11.0m,
            250.0m,
            Seq("CCS"),
            "Sedan",
            40000.0m).IfLeft(err => throw new Exception(err));

        // Act - create a modified copy using with expression
        var modified = ev with { PriceGbp = 45000.0m };

        // Assert
        ev.PriceGbp.Should().Be(40000.0m, "original should not be modified");
        modified.PriceGbp.Should().Be(45000.0m, "modified copy should have new price");
        ev.Id.Should().Be(modified.Id, "other properties should be the same");
    }
}
