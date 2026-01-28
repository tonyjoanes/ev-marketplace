namespace EvMarketplace.Domain.Models;

/// <summary>
/// Subscription tiers for sellers on the platform
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free tier - 1 listing, basic features
    /// </summary>
    Free = 0,

    /// <summary>
    /// Pro tier - £49/month - 10 listings, featured placement rotation
    /// </summary>
    Pro = 1,

    /// <summary>
    /// Premium tier - £149/month - Unlimited listings, priority placement, analytics
    /// </summary>
    Premium = 2,

    /// <summary>
    /// Enterprise tier - £299/month - API access, dedicated support, white-label
    /// </summary>
    Enterprise = 3
}
