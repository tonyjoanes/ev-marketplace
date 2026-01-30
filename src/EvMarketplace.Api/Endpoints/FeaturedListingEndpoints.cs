using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using EvMarketplace.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvMarketplace.Api.Endpoints;

public static class FeaturedListingEndpoints
{
    public static void MapFeaturedListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/featured-listings").WithTags("Featured Listings");

        group.MapPost("/", CreateFeaturedListing)
            .WithName("CreateFeaturedListing")
            .WithOpenApi();

        group.MapPost("/checkout", CreateFeaturedCheckoutSession)
            .WithName("CreateFeaturedCheckoutSession")
            .WithOpenApi();

        group.MapGet("/active/{type}", GetActiveFeaturedListings)
            .WithName("GetActiveFeaturedListings")
            .WithOpenApi();

        group.MapGet("/seller/{sellerId:guid}", GetSellerFeaturedListings)
            .WithName("GetSellerFeaturedListings")
            .WithOpenApi();

        group.MapPost("/{id:guid}/cancel", CancelFeaturedListing)
            .WithName("CancelFeaturedListing")
            .WithOpenApi();

        group.MapGet("/pricing", GetFeaturedPricing)
            .WithName("GetFeaturedPricing")
            .WithOpenApi();
    }

    private static IResult GetFeaturedPricing()
    {
        var pricing = new[]
        {
            new
            {
                Type = FeaturedType.HomepageHero,
                Name = "Homepage Hero",
                Description = "Top placement on homepage with large image",
                PricePerWeek = 99m,
                Currency = "GBP"
            },
            new
            {
                Type = FeaturedType.CategoryTop,
                Name = "Category Top",
                Description = "Top of category search results",
                PricePerWeek = 49m,
                Currency = "GBP"
            },
            new
            {
                Type = FeaturedType.SearchBoost,
                Name = "Search Boost",
                Description = "Boosted position in search results",
                PricePerWeek = 29m,
                Currency = "GBP"
            },
            new
            {
                Type = FeaturedType.Spotlight,
                Name = "Spotlight Badge",
                Description = "Special spotlight badge on listing",
                PricePerWeek = 15m,
                Currency = "GBP"
            }
        };

        return Results.Ok(pricing);
    }

    private static async Task<IResult> CreateFeaturedListing(
        [FromBody] CreateFeaturedListingRequest request,
        IFeaturedListingRepository featuredRepository,
        IListingRepository listingRepository,
        ISellerRepository sellerRepository)
    {
        // Verify listing exists and belongs to seller
        var listingOption = await listingRepository.GetByIdAsync(request.ListingId);

        if (listingOption.IsNone)
            return Results.NotFound(new { error = "Listing not found" });

        // Safe: listing exists (verified by IsNone check above)
        var listing = listingOption.IfNone(() => default(VehicleListing)!);

        // Verify seller
        var sellerOption = await sellerRepository.GetByIdAsync(request.SellerId);

        if (sellerOption.IsNone)
            return Results.NotFound(new { error = "Seller not found" });

        if (listing.SellerId != request.SellerId)
            return Results.BadRequest(new { error = "Listing does not belong to this seller" });

        // Create featured listing
        var featuredResult = FeaturedListing.Create(
            Guid.NewGuid(),
            request.ListingId,
            request.SellerId,
            request.Type,
            request.StartDate,
            request.DurationDays
        );

        return await featuredResult.Match(
            Right: async featured =>
            {
                var addResult = await featuredRepository.AddAsync(featured);

                return addResult.Match(
                    Right: created => Results.Created($"/api/featured-listings/{created.Id}", new
                    {
                        featured = created,
                        message = "Featured listing created. Complete payment to activate."
                    }),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }

    private static async Task<IResult> CreateFeaturedCheckoutSession(
        [FromBody] CreateFeaturedCheckoutRequest request,
        IFeaturedListingRepository featuredRepository,
        ISellerRepository sellerRepository,
        IStripeService stripeService)
    {
        // Get featured listing
        var featuredOption = await featuredRepository.GetByIdAsync(request.FeaturedListingId);

        if (featuredOption.IsNone)
            return Results.NotFound(new { error = "Featured listing not found" });

        // Safe: featured listing exists (verified by IsNone check above)
        var featured = featuredOption.IfNone(() => default(FeaturedListing)!);

        // Get seller
        var sellerOption = await sellerRepository.GetByIdAsync(featured.SellerId);

        if (sellerOption.IsNone)
            return Results.NotFound(new { error = "Seller not found" });

        // Safe: seller exists (verified by IsNone check above)
        var seller = sellerOption.IfNone(() => default(Seller)!);

        if (string.IsNullOrEmpty(seller.StripeCustomerId))
            return Results.BadRequest(new { error = "Seller must have a Stripe customer ID" });

        // Calculate duration
        var durationDays = (int)(featured.EndDate - featured.StartDate).TotalDays;

        // Create checkout session
        var sessionResult = await stripeService.CreateFeaturedListingCheckoutSessionAsync(
            seller.StripeCustomerId,
            featured.Type,
            durationDays,
            request.SuccessUrl,
            request.CancelUrl
        );

        return await sessionResult.Match(
            Right: session => Task.FromResult(Results.Ok(new
            {
                sessionId = session.Id,
                url = session.Url,
                amount = featured.PriceGbp
            })),
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }

    private static async Task<IResult> GetActiveFeaturedListings(
        FeaturedType type,
        IFeaturedListingRepository featuredRepository)
    {
        var result = await featuredRepository.GetActiveByTypeAsync(type);

        return result.Match(
            Right: featured => Results.Ok(featured),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetSellerFeaturedListings(
        Guid sellerId,
        IFeaturedListingRepository featuredRepository)
    {
        var result = await featuredRepository.GetBySellerIdAsync(sellerId);

        return result.Match(
            Right: featured => Results.Ok(featured),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> CancelFeaturedListing(
        Guid id,
        IFeaturedListingRepository featuredRepository)
    {
        var featuredOption = await featuredRepository.GetByIdAsync(id);

        if (featuredOption.IsNone)
            return Results.NotFound(new { error = "Featured listing not found" });

        // Safe: featured listing exists (verified by IsNone check above)
        var featured = featuredOption.IfNone(() => default(FeaturedListing)!);

        var canceled = featured.Cancel();
        var updateResult = await featuredRepository.UpdateAsync(canceled);

        return updateResult.Match(
            Right: f => Results.Ok(new
            {
                message = "Featured listing canceled successfully",
                featured = f
            }),
            Left: error => Results.Problem(error)
        );
    }
}

// Request DTOs
public record CreateFeaturedListingRequest(
    Guid ListingId,
    Guid SellerId,
    FeaturedType Type,
    DateTime StartDate,
    int DurationDays
);

public record CreateFeaturedCheckoutRequest(
    Guid FeaturedListingId,
    string SuccessUrl,
    string CancelUrl
);
