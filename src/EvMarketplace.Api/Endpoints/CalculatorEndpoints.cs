using EvMarketplace.Domain.Calculators;
using Microsoft.AspNetCore.Mvc;

namespace EvMarketplace.Api.Endpoints;

public static class CalculatorEndpoints
{
    public static void MapCalculatorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/calculators")
            .WithTags("Calculators")
            .WithOpenApi();

        group.MapPost("/running-cost", CalculateRunningCost)
            .WithName("CalculateRunningCost")
            .Produces<decimal>()
            .Produces(400);

        group.MapPost("/charging-time", CalculateChargingTime)
            .WithName("CalculateChargingTime")
            .Produces<decimal>()
            .Produces(400);

        group.MapPost("/tco", CalculateTco)
            .WithName("CalculateTco")
            .Produces<TcoResult>()
            .Produces(400);

        group.MapPost("/range-anxiety", CalculateRangeAnxiety)
            .WithName("CalculateRangeAnxiety")
            .Produces<decimal>()
            .Produces(400);
    }

    private static IResult CalculateRunningCost([FromBody] RunningCostRequest request)
    {
        var result = EvCalculators.CalculateMonthlyRunningCost(
            request.EfficiencyKwhPer100Km,
            request.AnnualMileageKm,
            request.ElectricityCostPencePerKwh
        );

        return result.Match(
            Right: cost => Results.Ok(new { MonthlyCostGbp = cost }),
            Left: error => Results.BadRequest(new { Error = error })
        );
    }

    private static IResult CalculateChargingTime([FromBody] ChargingTimeRequest request)
    {
        var result = EvCalculators.CalculateChargingTime10To80(
            request.BatteryCapacityKwh,
            request.ChargerPowerKw
        );

        return result.Match(
            Right: time => Results.Ok(new { ChargingTimeHours = time }),
            Left: error => Results.BadRequest(new { Error = error })
        );
    }

    private static IResult CalculateTco([FromBody] TcoRequest request)
    {
        var result = EvCalculators.CalculateSimpleTco(
            request.PurchasePriceGbp,
            request.EfficiencyKwhPer100Km,
            request.AnnualMileageKm,
            request.ElectricityCostPencePerKwh,
            request.Years
        );

        return result.Match(
            Right: tco => Results.Ok(tco),
            Left: error => Results.BadRequest(new { Error = error })
        );
    }

    private static IResult CalculateRangeAnxiety([FromBody] RangeAnxietyRequest request)
    {
        var result = EvCalculators.CalculateRangeAnxietyFactor(
            request.WltpRangeKm,
            request.DailyCommuteKm
        );

        return result.Match(
            Right: factor => Results.Ok(new { BufferPercentage = factor }),
            Left: error => Results.BadRequest(new { Error = error })
        );
    }
}

// Request DTOs
public sealed record RunningCostRequest(
    decimal EfficiencyKwhPer100Km,
    decimal AnnualMileageKm,
    decimal ElectricityCostPencePerKwh
);

public sealed record ChargingTimeRequest(
    decimal BatteryCapacityKwh,
    decimal ChargerPowerKw
);

public sealed record TcoRequest(
    decimal PurchasePriceGbp,
    decimal EfficiencyKwhPer100Km,
    decimal AnnualMileageKm,
    decimal ElectricityCostPencePerKwh,
    int Years
);

public sealed record RangeAnxietyRequest(
    decimal WltpRangeKm,
    decimal DailyCommuteKm
);
