using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents a seller's active subscription
/// </summary>
public sealed record Subscription
{
    public required Guid Id { get; init; }
    public required Guid SellerId { get; init; }
    public required SubscriptionTier Tier { get; init; }
    public required SubscriptionStatus Status { get; init; }
    public required DateTime StartDate { get; init; }
    public required Option<DateTime> EndDate { get; init; }
    public required Option<DateTime> TrialEndDate { get; init; }
    public required DateTime CurrentPeriodStart { get; init; }
    public required DateTime CurrentPeriodEnd { get; init; }
    public required bool CancelAtPeriodEnd { get; init; }
    public required string StripeSubscriptionId { get; init; }
    public required string StripeCustomerId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Factory method to create a new subscription with validation
    /// </summary>
    public static Either<string, Subscription> Create(
        Guid id,
        Guid sellerId,
        SubscriptionTier tier,
        string stripeSubscriptionId,
        string stripeCustomerId,
        DateTime currentPeriodStart,
        DateTime currentPeriodEnd,
        Option<DateTime> trialEndDate = default)
    {
        var validations = Seq(
            ValidateId(id),
            ValidateSellerId(sellerId),
            ValidateStripeSubscriptionId(stripeSubscriptionId),
            ValidateStripeCustomerId(stripeCustomerId),
            ValidatePeriod(currentPeriodStart, currentPeriodEnd)
        );

        var errors = validations.Lefts().ToSeq();

        if (!errors.IsEmpty)
            return string.Join(", ", errors);

        var now = DateTime.UtcNow;

        return new Subscription
        {
            Id = id,
            SellerId = sellerId,
            Tier = tier,
            Status = SubscriptionStatus.Active,
            StartDate = now,
            EndDate = None,
            TrialEndDate = trialEndDate,
            CurrentPeriodStart = currentPeriodStart,
            CurrentPeriodEnd = currentPeriodEnd,
            CancelAtPeriodEnd = false,
            StripeSubscriptionId = stripeSubscriptionId,
            StripeCustomerId = stripeCustomerId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>
    /// Create a free subscription (no Stripe required)
    /// </summary>
    public static Either<string, Subscription> CreateFree(Guid id, Guid sellerId)
    {
        var now = DateTime.UtcNow;

        return ValidateId(id)
            .Bind(_ => ValidateSellerId(sellerId))
            .Map(_ => new Subscription
            {
                Id = id,
                SellerId = sellerId,
                Tier = SubscriptionTier.Free,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = None,
                TrialEndDate = None,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddYears(100), // Free is "forever"
                CancelAtPeriodEnd = false,
                StripeSubscriptionId = string.Empty,
                StripeCustomerId = string.Empty,
                CreatedAt = now,
                UpdatedAt = now
            });
    }

    // Validation functions
    private static Either<string, Unit> ValidateId(Guid id) =>
        id != Guid.Empty ? unit : "Subscription ID cannot be empty";

    private static Either<string, Unit> ValidateSellerId(Guid sellerId) =>
        sellerId != Guid.Empty ? unit : "Seller ID cannot be empty";

    private static Either<string, Unit> ValidateStripeSubscriptionId(string stripeSubscriptionId) =>
        !string.IsNullOrWhiteSpace(stripeSubscriptionId)
            ? unit
            : "Stripe subscription ID is required for paid subscriptions";

    private static Either<string, Unit> ValidateStripeCustomerId(string stripeCustomerId) =>
        !string.IsNullOrWhiteSpace(stripeCustomerId)
            ? unit
            : "Stripe customer ID is required for paid subscriptions";

    private static Either<string, Unit> ValidatePeriod(DateTime start, DateTime end) =>
        end > start ? unit : "Period end must be after period start";

    // Business logic methods

    /// <summary>
    /// Check if subscription is currently active and not expired
    /// </summary>
    public bool IsActive() =>
        Status == SubscriptionStatus.Active &&
        DateTime.UtcNow <= CurrentPeriodEnd;

    /// <summary>
    /// Check if subscription is in trial period
    /// </summary>
    public bool IsInTrial() =>
        TrialEndDate.IsSome &&
        TrialEndDate.Match(
            Some: trialEnd => DateTime.UtcNow <= trialEnd,
            None: () => false
        );

    /// <summary>
    /// Get the subscription plan details
    /// </summary>
    public Option<SubscriptionPlan> GetPlan() =>
        SubscriptionPlan.Plans.GetPlan(Tier);

    /// <summary>
    /// Cancel subscription at end of current period
    /// </summary>
    public Subscription ScheduleCancellation() =>
        this with
        {
            CancelAtPeriodEnd = true,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Reactivate a scheduled cancellation
    /// </summary>
    public Subscription UndoCancellation() =>
        this with
        {
            CancelAtPeriodEnd = false,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Upgrade or downgrade to a different tier
    /// </summary>
    public Subscription ChangeTier(SubscriptionTier newTier) =>
        this with
        {
            Tier = newTier,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Update subscription status
    /// </summary>
    public Subscription UpdateStatus(SubscriptionStatus newStatus) =>
        this with
        {
            Status = newStatus,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Renew subscription for next period
    /// </summary>
    public Subscription Renew(DateTime newPeriodStart, DateTime newPeriodEnd) =>
        this with
        {
            CurrentPeriodStart = newPeriodStart,
            CurrentPeriodEnd = newPeriodEnd,
            Status = SubscriptionStatus.Active,
            CancelAtPeriodEnd = false,
            UpdatedAt = DateTime.UtcNow
        };
}

/// <summary>
/// Subscription status
/// </summary>
public enum SubscriptionStatus
{
    Active,
    Trialing,
    PastDue,
    Canceled,
    Unpaid,
    Incomplete,
    IncompleteExpired
}
