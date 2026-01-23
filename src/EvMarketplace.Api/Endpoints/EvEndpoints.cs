using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using static LanguageExt.Prelude;

namespace EvMarketplace.Api.Endpoints;

public static class EvEndpoints
{
    public static void MapEvEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/vehicles")
            .WithTags("Electric Vehicles")
            .WithOpenApi();

        group.MapGet("/", GetAllVehicles)
            .WithName("GetAllVehicles")
            .Produces<List<ElectricVehicle>>();

        group.MapGet("/{id:guid}", GetVehicleById)
            .WithName("GetVehicleById")
            .Produces<ElectricVehicle>()
            .Produces(404);

        group.MapPost("/search", SearchVehicles)
            .WithName("SearchVehicles")
            .Produces<List<ElectricVehicle>>();

        group.MapPost("/", AddVehicle)
            .WithName("AddVehicle")
            .Produces<ElectricVehicle>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateVehicle)
            .WithName("UpdateVehicle")
            .Produces(204)
            .Produces(400);

        group.MapDelete("/{id:guid}", DeleteVehicle)
            .WithName("DeleteVehicle")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetAllVehicles(IEvRepository repository)
    {
        var result = await repository.GetAllAsync();
        return result.Match(
            Right: vehicles => Results.Ok(vehicles.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetVehicleById(Guid id, IEvRepository repository)
    {
        var result = await repository.GetByIdAsync(id);
        return result.Match(
            Some: vehicle => Results.Ok(vehicle),
            None: () => Results.NotFound()
        );
    }

    private static async Task<IResult> SearchVehicles(
        [FromBody] SearchRequest request,
        IEvRepository repository)
    {
        var criteria = new EvSearchCriteria(
            Make: request.Make.ToOption(),
            BodyType: request.BodyType.ToOption(),
            MinPrice: request.MinPrice.ToOption(),
            MaxPrice: request.MaxPrice.ToOption(),
            MinRange: request.MinRange.ToOption()
        );

        var result = await repository.SearchAsync(criteria);
        return result.Match(
            Right: vehicles => Results.Ok(vehicles.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> AddVehicle(
        [FromBody] CreateEvRequest request,
        IEvRepository repository)
    {
        var evResult = ElectricVehicle.Create(
            Guid.NewGuid(),
            request.Make,
            request.Model,
            request.Year,
            request.BatteryCapacityKwh,
            request.WltpRangeKm,
            request.EfficiencyKwhPer100Km,
            request.AcChargeRateKw,
            request.DcChargeRateKw,
            Seq(request.ConnectorTypes),
            request.BodyType,
            request.PriceGbp
        );

        return await evResult.Match(
            Right: async ev =>
            {
                var addResult = await repository.AddAsync(ev);
                return addResult.Match(
                    Right: created => Results.Created($"/api/vehicles/{created.Id}", created),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(error))
        );
    }

    private static async Task<IResult> UpdateVehicle(
        Guid id,
        [FromBody] CreateEvRequest request,
        IEvRepository repository)
    {
        var evResult = ElectricVehicle.Create(
            id,
            request.Make,
            request.Model,
            request.Year,
            request.BatteryCapacityKwh,
            request.WltpRangeKm,
            request.EfficiencyKwhPer100Km,
            request.AcChargeRateKw,
            request.DcChargeRateKw,
            Seq(request.ConnectorTypes),
            request.BodyType,
            request.PriceGbp
        );

        return await evResult.Match(
            Right: async ev =>
            {
                var updateResult = await repository.UpdateAsync(ev);
                return updateResult.Match(
                    Right: _ => Results.NoContent(),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(error))
        );
    }

    private static async Task<IResult> DeleteVehicle(Guid id, IEvRepository repository)
    {
        var result = await repository.DeleteAsync(id);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.NotFound(error)
        );
    }
}

// Request DTOs
public sealed record SearchRequest(
    string? Make,
    string? BodyType,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinRange
);

public sealed record CreateEvRequest(
    string Make,
    string Model,
    int Year,
    decimal BatteryCapacityKwh,
    decimal WltpRangeKm,
    decimal EfficiencyKwhPer100Km,
    decimal AcChargeRateKw,
    decimal DcChargeRateKw,
    string[] ConnectorTypes,
    string BodyType,
    decimal PriceGbp
);

// Extension method to convert nullable to Option
public static class OptionExtensions
{
    public static Option<T> ToOption<T>(this T? value) where T : class =>
        value is null ? None : Some(value);

    public static Option<T> ToOption<T>(this T? value) where T : struct =>
        value.HasValue ? Some(value.Value) : None;
}
