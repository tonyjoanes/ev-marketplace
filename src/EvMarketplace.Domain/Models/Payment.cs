using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents a payment transaction on the platform
/// </summary>
public sealed record Payment
{
    public required Guid Id { get; init; }
    public required Guid SellerId { get; init; }
    public required PaymentType Type { get; init; }
    public required decimal AmountGbp { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string StripePaymentIntentId { get; init; }
    public required Option<Guid> SubscriptionId { get; init; }
    public required Option<Guid> FeaturedListingId { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required Option<DateTime> PaidAt { get; init; }

    /// <summary>
    /// Factory method to create a payment with validation
    /// </summary>
    public static Either<string, Payment> Create(
        Guid id,
        Guid sellerId,
        PaymentType type,
        decimal amountGbp,
        string stripePaymentIntentId,
        string description,
        Option<Guid> subscriptionId = default,
        Option<Guid> featuredListingId = default)
    {
        var validations = Seq(
            ValidateId(id),
            ValidateSellerId(sellerId),
            ValidateAmount(amountGbp),
            ValidateStripePaymentIntentId(stripePaymentIntentId),
            ValidateDescription(description)
        );

        var errors = validations.Lefts().ToSeq();

        if (!errors.IsEmpty)
            return string.Join(", ", errors);

        var now = DateTime.UtcNow;

        return new Payment
        {
            Id = id,
            SellerId = sellerId,
            Type = type,
            AmountGbp = amountGbp,
            Status = PaymentStatus.Pending,
            StripePaymentIntentId = stripePaymentIntentId,
            SubscriptionId = subscriptionId,
            FeaturedListingId = featuredListingId,
            Description = description,
            CreatedAt = now,
            UpdatedAt = now,
            PaidAt = None
        };
    }

    // Validation functions
    private static Either<string, Unit> ValidateId(Guid id) =>
        id != Guid.Empty ? unit : "Payment ID cannot be empty";

    private static Either<string, Unit> ValidateSellerId(Guid sellerId) =>
        sellerId != Guid.Empty ? unit : "Seller ID cannot be empty";

    private static Either<string, Unit> ValidateAmount(decimal amount) =>
        amount > 0 ? unit : "Payment amount must be greater than zero";

    private static Either<string, Unit> ValidateStripePaymentIntentId(string intentId) =>
        !string.IsNullOrWhiteSpace(intentId) ? unit : "Stripe payment intent ID is required";

    private static Either<string, Unit> ValidateDescription(string description) =>
        !string.IsNullOrWhiteSpace(description) && description.Length <= 500
            ? unit
            : "Description is required and must be less than 500 characters";

    // Business logic methods

    /// <summary>
    /// Mark payment as succeeded
    /// </summary>
    public Payment MarkAsSucceeded() =>
        this with
        {
            Status = PaymentStatus.Succeeded,
            PaidAt = Some(DateTime.UtcNow),
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Mark payment as failed
    /// </summary>
    public Payment MarkAsFailed() =>
        this with
        {
            Status = PaymentStatus.Failed,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Mark payment as refunded
    /// </summary>
    public Payment MarkAsRefunded() =>
        this with
        {
            Status = PaymentStatus.Refunded,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Check if payment was successful
    /// </summary>
    public bool IsSuccessful() => Status == PaymentStatus.Succeeded;
}

/// <summary>
/// Type of payment transaction
/// </summary>
public enum PaymentType
{
    Subscription,
    FeaturedListing,
    SingleListing,
    LeadPurchase
}

/// <summary>
/// Payment status (matches Stripe payment intent statuses)
/// </summary>
public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Canceled,
    Refunded
}
