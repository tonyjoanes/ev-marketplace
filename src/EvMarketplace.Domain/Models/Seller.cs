using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents a seller (dealer or private individual)
/// </summary>
public sealed record Seller
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required SellerType Type { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required Option<string> CompanyName { get; init; }
    public required Option<string> Location { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsVerified { get; init; }

    // Subscription-related fields
    public required SubscriptionTier CurrentSubscriptionTier { get; init; }
    public required Option<Guid> ActiveSubscriptionId { get; init; }
    public required string StripeCustomerId { get; init; }
    public required int CurrentListingCount { get; init; }

    public static Either<string, Seller> Create(
        Guid id,
        string name,
        SellerType type,
        string email,
        string phoneNumber,
        Option<string> companyName = default,
        Option<string> location = default,
        DateTime? createdAt = null,
        bool isVerified = false,
        string stripeCustomerId = "",
        SubscriptionTier subscriptionTier = SubscriptionTier.Free)
    {
        var validations = Seq(
            ValidateName(name),
            ValidateEmail(email),
            ValidatePhoneNumber(phoneNumber),
            ValidateCompanyName(type, companyName)
        );

        var errors = validations.Lefts().ToSeq();

        return errors.IsEmpty
            ? new Seller
            {
                Id = id,
                Name = name,
                Type = type,
                Email = email,
                PhoneNumber = phoneNumber,
                CompanyName = companyName,
                Location = location,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsVerified = isVerified,
                CurrentSubscriptionTier = subscriptionTier,
                ActiveSubscriptionId = None,
                StripeCustomerId = stripeCustomerId,
                CurrentListingCount = 0
            }
            : string.Join(", ", errors);
    }

    private static Either<string, Unit> ValidateName(string name) =>
        string.IsNullOrWhiteSpace(name)
            ? "Name cannot be empty"
            : unit;

    private static Either<string, Unit> ValidateEmail(string email) =>
        string.IsNullOrWhiteSpace(email) || !email.Contains("@")
            ? "Valid email is required"
            : unit;

    private static Either<string, Unit> ValidatePhoneNumber(string phone) =>
        string.IsNullOrWhiteSpace(phone)
            ? "Phone number is required"
            : unit;

    private static Either<string, Unit> ValidateCompanyName(
        SellerType type,
        Option<string> companyName) =>
        type == SellerType.Dealer && companyName.IsNone
            ? "Company name is required for dealers"
            : unit;

    // Business logic methods

    /// <summary>
    /// Check if seller can add more listings based on their subscription
    /// </summary>
    public bool CanAddListing() =>
        SubscriptionPlan.Plans.GetPlan(CurrentSubscriptionTier)
            .Match(
                Some: plan => plan.CanAddListing(CurrentListingCount),
                None: () => false
            );

    /// <summary>
    /// Get remaining listings available
    /// </summary>
    public int GetRemainingListings() =>
        SubscriptionPlan.Plans.GetPlan(CurrentSubscriptionTier)
            .Match(
                Some: plan => plan.GetRemainingListings(CurrentListingCount),
                None: () => 0
            );

    /// <summary>
    /// Increment listing count
    /// </summary>
    public Seller IncrementListingCount() =>
        this with { CurrentListingCount = CurrentListingCount + 1 };

    /// <summary>
    /// Decrement listing count
    /// </summary>
    public Seller DecrementListingCount() =>
        this with { CurrentListingCount = Math.Max(0, CurrentListingCount - 1) };

    /// <summary>
    /// Update subscription tier
    /// </summary>
    public Seller UpdateSubscription(SubscriptionTier newTier, Guid subscriptionId, string stripeCustomerId) =>
        this with
        {
            CurrentSubscriptionTier = newTier,
            ActiveSubscriptionId = Some(subscriptionId),
            StripeCustomerId = stripeCustomerId
        };

    /// <summary>
    /// Cancel subscription (revert to free tier)
    /// </summary>
    public Seller CancelSubscription() =>
        this with
        {
            CurrentSubscriptionTier = SubscriptionTier.Free,
            ActiveSubscriptionId = None
        };
}

public enum SellerType
{
    Private,
    Dealer
}
