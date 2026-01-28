using LanguageExt;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Defines the features and limits for each subscription tier
/// </summary>
public sealed record SubscriptionPlan
{
    public required SubscriptionTier Tier { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal PriceGbpPerMonth { get; init; }
    public required int MaxListings { get; init; } // -1 for unlimited
    public required bool FeaturedPlacementRotation { get; init; }
    public required bool PriorityPlacement { get; init; }
    public required bool AnalyticsDashboard { get; init; }
    public required bool ApiAccess { get; init; }
    public required bool DedicatedSupport { get; init; }
    public required int LeadsPerMonth { get; init; } // -1 for unlimited
    public required string StripeProductId { get; init; }
    public required string StripePriceId { get; init; }

    /// <summary>
    /// Predefined subscription plans
    /// </summary>
    public static class Plans
    {
        public static SubscriptionPlan Free => new()
        {
            Tier = SubscriptionTier.Free,
            Name = "Free",
            Description = "Perfect for trying out the platform",
            PriceGbpPerMonth = 0m,
            MaxListings = 1,
            FeaturedPlacementRotation = false,
            PriorityPlacement = false,
            AnalyticsDashboard = false,
            ApiAccess = false,
            DedicatedSupport = false,
            LeadsPerMonth = 5,
            StripeProductId = string.Empty,
            StripePriceId = string.Empty
        };

        public static SubscriptionPlan Pro => new()
        {
            Tier = SubscriptionTier.Pro,
            Name = "Pro",
            Description = "For individual dealers and small businesses",
            PriceGbpPerMonth = 49m,
            MaxListings = 10,
            FeaturedPlacementRotation = true,
            PriorityPlacement = false,
            AnalyticsDashboard = true,
            ApiAccess = false,
            DedicatedSupport = false,
            LeadsPerMonth = 50,
            StripeProductId = "prod_pro", // Will be replaced with actual Stripe IDs
            StripePriceId = "price_pro"
        };

        public static SubscriptionPlan Premium => new()
        {
            Tier = SubscriptionTier.Premium,
            Name = "Premium",
            Description = "For established dealers with high volume",
            PriceGbpPerMonth = 149m,
            MaxListings = -1, // Unlimited
            FeaturedPlacementRotation = true,
            PriorityPlacement = true,
            AnalyticsDashboard = true,
            ApiAccess = false,
            DedicatedSupport = false,
            LeadsPerMonth = -1, // Unlimited
            StripeProductId = "prod_premium",
            StripePriceId = "price_premium"
        };

        public static SubscriptionPlan Enterprise => new()
        {
            Tier = SubscriptionTier.Enterprise,
            Name = "Enterprise",
            Description = "For large dealers and franchises",
            PriceGbpPerMonth = 299m,
            MaxListings = -1, // Unlimited
            FeaturedPlacementRotation = true,
            PriorityPlacement = true,
            AnalyticsDashboard = true,
            ApiAccess = true,
            DedicatedSupport = true,
            LeadsPerMonth = -1, // Unlimited
            StripeProductId = "prod_enterprise",
            StripePriceId = "price_enterprise"
        };

        public static Seq<SubscriptionPlan> AllPlans => Seq(Free, Pro, Premium, Enterprise);

        public static Option<SubscriptionPlan> GetPlan(SubscriptionTier tier) =>
            AllPlans.Find(p => p.Tier == tier);
    }

    /// <summary>
    /// Check if the subscription allows adding more listings
    /// </summary>
    public bool CanAddListing(int currentListingCount) =>
        MaxListings == -1 || currentListingCount < MaxListings;

    /// <summary>
    /// Get remaining listings available
    /// </summary>
    public int GetRemainingListings(int currentListingCount) =>
        MaxListings == -1 ? int.MaxValue : Math.Max(0, MaxListings - currentListingCount);
}
