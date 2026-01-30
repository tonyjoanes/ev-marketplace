using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;
using static LanguageExt.Prelude;

namespace EvMarketplace.Api.Endpoints;

public static class ListingEndpoints
{
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/listings")
            .WithTags("Vehicle Listings")
            .WithOpenApi();

        group.MapGet("/", GetActiveListings)
            .WithName("GetActiveListings")
            .Produces<List<VehicleListing>>();

        group.MapGet("/{id:guid}", GetListingById)
            .WithName("GetListingById")
            .Produces<VehicleListing>()
            .Produces(404);

        group.MapGet("/seller/{sellerId:guid}", GetSellerListings)
            .WithName("GetSellerListings")
            .Produces<List<VehicleListing>>();

        group.MapPost("/search", SearchListings)
            .WithName("SearchListings")
            .Produces<List<VehicleListing>>();

        group.MapPost("/", CreateListing)
            .WithName("CreateListing")
            .Produces<VehicleListing>(201)
            .Produces(400);

        group.MapPut("/{id:guid}", UpdateListing)
            .WithName("UpdateListing")
            .Produces(204)
            .Produces(400);

        group.MapDelete("/{id:guid}", DeleteListing)
            .WithName("DeleteListing")
            .Produces(204)
            .Produces(404);

        group.MapPost("/{id:guid}/mark-sold", MarkListingAsSold)
            .WithName("MarkListingAsSold")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetActiveListings(IListingRepository repository)
    {
        var result = await repository.GetActiveListingsAsync();
        return result.Match(
            Right: listings => Results.Ok(listings.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetListingById(Guid id, IListingRepository repository)
    {
        var result = await repository.GetByIdAsync(id);
        return result.Match(
            Some: listing => Results.Ok(listing),
            None: () => Results.NotFound()
        );
    }

    private static async Task<IResult> GetSellerListings(Guid sellerId, IListingRepository repository)
    {
        var result = await repository.GetBySellerAsync(sellerId);
        return result.Match(
            Right: listings => Results.Ok(listings.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> SearchListings(
        [FromBody] ListingSearchRequest request,
        IListingRepository repository)
    {
        var criteria = new ListingSearchCriteria(
            Make: request.Make.ToOption(),
            BodyType: request.BodyType.ToOption(),
            Condition: request.Condition.HasValue ? Some((VehicleCondition)request.Condition.Value) : None,
            MinPrice: request.MinPrice.ToOption(),
            MaxPrice: request.MaxPrice.ToOption(),
            MaxMileage: request.MaxMileage.ToOption(),
            Location: request.Location.ToOption()
        );

        var result = await repository.SearchAsync(criteria);
        return result.Match(
            Right: listings => Results.Ok(listings.ToList()),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> CreateListing(
        [FromBody] CreateListingRequest request,
        IListingRepository repository)
    {
        var listingResult = VehicleListing.Create(
            Guid.NewGuid(),
            request.SellerId,
            request.Make,
            request.Model,
            request.Year,
            request.BatteryCapacityKwh,
            request.WltpRangeKm,
            request.EfficiencyKwhPer100Km,
            request.BodyType,
            (VehicleCondition)request.Condition,
            request.Mileage,
            request.AskingPriceGbp,
            request.Location,
            request.Description,
            Seq(request.ImageUrls ?? Array.Empty<string>()),
            request.CatalogueVehicleId.ToOption()
        );

        return await listingResult.Match(
            Right: async listing =>
            {
                var addResult = await repository.AddAsync(listing);
                return addResult.Match(
                    Right: created => Results.Created($"/api/listings/{created.Id}", created),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(error))
        );
    }

    private static async Task<IResult> UpdateListing(
        Guid id,
        [FromBody] UpdateListingRequest request,
        IListingRepository repository)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing.IsNone)
            return Results.NotFound();

        // Safe: listing exists (verified by IsNone check above)
        var existingListing = existing.IfNone(() => default(VehicleListing)!);
        var listing = existingListing with
        {
            AskingPriceGbp = request.AskingPriceGbp,
            Description = request.Description,
            Location = request.Location,
            Mileage = request.Mileage,
            Status = (ListingStatus)request.Status
        };

        var result = await repository.UpdateAsync(listing);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> DeleteListing(Guid id, IListingRepository repository)
    {
        var result = await repository.DeleteAsync(id);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.NotFound(error)
        );
    }

    private static async Task<IResult> MarkListingAsSold(Guid id, IListingRepository repository)
    {
        var result = await repository.MarkAsSoldAsync(id);
        return result.Match(
            Right: _ => Results.NoContent(),
            Left: error => Results.NotFound(error)
        );
    }
}

// Request DTOs
public sealed record ListingSearchRequest(
    string? Make,
    string? BodyType,
    int? Condition,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MaxMileage,
    string? Location
);

public sealed record CreateListingRequest(
    Guid SellerId,
    string Make,
    string Model,
    int Year,
    decimal BatteryCapacityKwh,
    decimal WltpRangeKm,
    decimal EfficiencyKwhPer100Km,
    string BodyType,
    int Condition,
    decimal Mileage,
    decimal AskingPriceGbp,
    string Location,
    string Description,
    string[]? ImageUrls,
    Guid? CatalogueVehicleId
);

public sealed record UpdateListingRequest(
    decimal AskingPriceGbp,
    string Description,
    string Location,
    decimal Mileage,
    int Status
);
