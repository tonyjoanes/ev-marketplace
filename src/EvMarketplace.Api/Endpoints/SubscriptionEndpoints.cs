using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Repositories;
using EvMarketplace.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvMarketplace.Api.Endpoints;

public static class SubscriptionEndpoints
{
    public static void MapSubscriptionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapGet("/plans", GetSubscriptionPlans)
            .WithName("GetSubscriptionPlans")
            .WithOpenApi();

        group.MapPost("/checkout", CreateCheckoutSession)
            .WithName("CreateCheckoutSession")
            .WithOpenApi();

        group.MapPost("/portal", CreatePortalSession)
            .WithName("CreatePortalSession")
            .WithOpenApi();

        group.MapGet("/seller/{sellerId:guid}", GetSellerSubscription)
            .WithName("GetSellerSubscription")
            .WithOpenApi();

        group.MapPost("/cancel", CancelSubscription)
            .WithName("CancelSubscription")
            .WithOpenApi();

        group.MapPost("/upgrade", UpgradeSubscription)
            .WithName("UpgradeSubscription")
            .WithOpenApi();
    }

    private static IResult GetSubscriptionPlans()
    {
        var plans = SubscriptionPlan.Plans.AllPlans
            .Select(p => new
            {
                p.Tier,
                p.Name,
                p.Description,
                PriceGbpPerMonth = p.PriceGbpPerMonth,
                MaxListings = p.MaxListings == -1 ? "Unlimited" : p.MaxListings.ToString(),
                p.FeaturedPlacementRotation,
                p.PriorityPlacement,
                p.AnalyticsDashboard,
                p.ApiAccess,
                p.DedicatedSupport,
                LeadsPerMonth = p.LeadsPerMonth == -1 ? "Unlimited" : p.LeadsPerMonth.ToString()
            })
            .ToList();

        return Results.Ok(plans);
    }

    private static async Task<IResult> CreateCheckoutSession(
        [FromBody] CreateCheckoutSessionRequest request,
        ISellerRepository sellerRepository,
        IStripeService stripeService)
    {
        // Get seller
        var sellerOption = await sellerRepository.GetByIdAsync(request.SellerId);

        if (sellerOption.IsNone)
            return Results.NotFound(new { error = "Seller not found" });

        // Safe: seller exists (verified by IsNone check above)
        var seller = sellerOption.IfNone(() => default(Seller)!);

        // Create Stripe customer if doesn't exist
        string customerId = seller.StripeCustomerId;

        if (string.IsNullOrEmpty(customerId))
        {
            var customerResult = await stripeService.CreateCustomerAsync(seller.Email, seller.Name);

            if (customerResult.IsLeft)
                return Results.BadRequest(new { error = customerResult.Match(l => l, r => "") });

            customerId = customerResult.Match(l => "", r => r.Id);

            // Update seller with Stripe customer ID
            var updatedSeller = seller with { StripeCustomerId = customerId };
            await sellerRepository.UpdateAsync(updatedSeller);
        }

        // Create checkout session
        var sessionResult = await stripeService.CreateCheckoutSessionAsync(
            customerId,
            request.Tier,
            request.SuccessUrl,
            request.CancelUrl
        );

        return await sessionResult.Match(
            Right: session => Task.FromResult(Results.Ok(new
            {
                sessionId = session.Id,
                url = session.Url
            })),
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }

    private static async Task<IResult> CreatePortalSession(
        [FromBody] CreatePortalSessionRequest request,
        ISellerRepository sellerRepository,
        IStripeService stripeService)
    {
        var sellerOption = await sellerRepository.GetByIdAsync(request.SellerId);

        if (sellerOption.IsNone)
            return Results.NotFound(new { error = "Seller not found" });

        // Safe: seller exists (verified by IsNone check above)
        var seller = sellerOption.IfNone(() => default(Seller)!);

        if (string.IsNullOrEmpty(seller.StripeCustomerId))
            return Results.BadRequest(new { error = "Seller has no Stripe customer ID" });

        var sessionResult = await stripeService.CreatePortalSessionAsync(
            seller.StripeCustomerId,
            request.ReturnUrl
        );

        return await sessionResult.Match(
            Right: session => Task.FromResult(Results.Ok(new { url = session.Url })),
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }

    private static async Task<IResult> GetSellerSubscription(
        Guid sellerId,
        ISubscriptionRepository subscriptionRepository)
    {
        var subscriptionOption = await subscriptionRepository.GetBySellerIdAsync(sellerId);

        return subscriptionOption.Match(
            Some: subscription => Results.Ok(subscription),
            None: () => Results.NotFound(new { error = "No active subscription found" })
        );
    }

    private static async Task<IResult> CancelSubscription(
        [FromBody] CancelSubscriptionRequest request,
        ISubscriptionRepository subscriptionRepository,
        ISellerRepository sellerRepository,
        IStripeService stripeService)
    {
        var subscriptionOption = await subscriptionRepository.GetByIdAsync(request.SubscriptionId);

        if (subscriptionOption.IsNone)
            return Results.NotFound(new { error = "Subscription not found" });

        // Safe: subscription exists (verified by IsNone check above)
        var subscription = subscriptionOption.IfNone(() => default(Subscription)!);

        // Cancel in Stripe
        var cancelResult = await stripeService.CancelSubscriptionAsync(
            subscription.StripeSubscriptionId,
            request.CancelImmediately
        );

        return await cancelResult.Match(
            Right: async stripeSubscription =>
            {
                // Update local subscription
                var updatedSubscription = request.CancelImmediately
                    ? subscription.UpdateStatus(SubscriptionStatus.Canceled)
                    : subscription.ScheduleCancellation();

                var updateResult = await subscriptionRepository.UpdateAsync(updatedSubscription);

                return updateResult.Match(
                    Right: sub => Results.Ok(new
                    {
                        message = request.CancelImmediately
                            ? "Subscription canceled immediately"
                            : "Subscription will cancel at period end",
                        subscription = sub
                    }),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }

    private static async Task<IResult> UpgradeSubscription(
        [FromBody] UpgradeSubscriptionRequest request,
        ISubscriptionRepository subscriptionRepository,
        IStripeService stripeService)
    {
        var subscriptionOption = await subscriptionRepository.GetByIdAsync(request.SubscriptionId);

        if (subscriptionOption.IsNone)
            return Results.NotFound(new { error = "Subscription not found" });

        // Safe: subscription exists (verified by IsNone check above)
        var subscription = subscriptionOption.IfNone(() => default(Subscription)!);

        // Get new plan
        var newPlanOption = SubscriptionPlan.Plans.GetPlan(request.NewTier);

        if (newPlanOption.IsNone)
            return Results.BadRequest(new { error = "Invalid subscription tier" });

        // Safe: plan exists (verified by IsNone check above)
        var newPlan = newPlanOption.IfNone(() => default(SubscriptionPlan)!);

        // Update in Stripe
        var updateResult = await stripeService.UpdateSubscriptionAsync(
            subscription.StripeSubscriptionId,
            newPlan.StripePriceId
        );

        return await updateResult.Match(
            Right: async stripeSubscription =>
            {
                // Update local subscription
                var updatedSubscription = subscription.ChangeTier(request.NewTier);
                var saveResult = await subscriptionRepository.UpdateAsync(updatedSubscription);

                return saveResult.Match(
                    Right: sub => Results.Ok(new
                    {
                        message = "Subscription upgraded successfully",
                        subscription = sub
                    }),
                    Left: error => Results.Problem(error)
                );
            },
            Left: error => Task.FromResult(Results.BadRequest(new { error }))
        );
    }
}

// Request DTOs
public record CreateCheckoutSessionRequest(
    Guid SellerId,
    SubscriptionTier Tier,
    string SuccessUrl,
    string CancelUrl
);

public record CreatePortalSessionRequest(
    Guid SellerId,
    string ReturnUrl
);

public record CancelSubscriptionRequest(
    Guid SubscriptionId,
    bool CancelImmediately
);

public record UpgradeSubscriptionRequest(
    Guid SubscriptionId,
    SubscriptionTier NewTier
);
