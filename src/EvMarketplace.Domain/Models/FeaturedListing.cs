using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Domain.Models;

/// <summary>
/// Represents a featured/promoted listing upgrade
/// </summary>
public sealed record FeaturedListing
{
    public required Guid Id { get; init; }
    public required Guid ListingId { get; init; }
    public required Guid SellerId { get; init; }
    public required FeaturedType Type { get; init; }
    public required decimal PriceGbp { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required FeaturedStatus Status { get; init; }
    public required Option<Guid> PaymentId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Factory method to create a featured listing with validation
    /// </summary>
    public static Either<string, FeaturedListing> Create(
        Guid id,
        Guid listingId,
        Guid sellerId,
        FeaturedType type,
        DateTime startDate,
        int durationDays)
    {
        var validations = Seq(
            ValidateId(id),
            ValidateListingId(listingId),
            ValidateSellerId(sellerId),
            ValidateDuration(durationDays)
        );

        var errors = validations.Lefts().ToSeq();

        if (!errors.IsEmpty)
            return string.Join(", ", errors);

        var now = DateTime.UtcNow;
        var endDate = startDate.AddDays(durationDays);

        return new FeaturedListing
        {
            Id = id,
            ListingId = listingId,
            SellerId = sellerId,
            Type = type,
            PriceGbp = GetPriceForType(type, durationDays),
            StartDate = startDate,
            EndDate = endDate,
            Status = FeaturedStatus.Pending,
            PaymentId = None,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    // Validation functions
    private static Either<string, Unit> ValidateId(Guid id) =>
        id != Guid.Empty ? unit : "Featured listing ID cannot be empty";

    private static Either<string, Unit> ValidateListingId(Guid listingId) =>
        listingId != Guid.Empty ? unit : "Listing ID cannot be empty";

    private static Either<string, Unit> ValidateSellerId(Guid sellerId) =>
        sellerId != Guid.Empty ? unit : "Seller ID cannot be empty";

    private static Either<string, Unit> ValidateDuration(int durationDays) =>
        durationDays > 0 && durationDays <= 365 ? unit : "Duration must be between 1 and 365 days";

    /// <summary>
    /// Calculate price based on featured type and duration
    /// </summary>
    private static decimal GetPriceForType(FeaturedType type, int durationDays)
    {
        var weeklyPrice = type switch
        {
            FeaturedType.HomepageHero => 99m,
            FeaturedType.CategoryTop => 49m,
            FeaturedType.SearchBoost => 29m,
            FeaturedType.Spotlight => 15m,
            _ => 0m
        };

        var weeks = Math.Ceiling(durationDays / 7.0m);
        return weeklyPrice * weeks;
    }

    // Business logic methods

    /// <summary>
    /// Check if featured listing is currently active
    /// </summary>
    public bool IsActive()
    {
        var now = DateTime.UtcNow;
        return Status == FeaturedStatus.Active &&
               now >= StartDate &&
               now <= EndDate;
    }

    /// <summary>
    /// Check if featured listing has expired
    /// </summary>
    public bool IsExpired() =>
        DateTime.UtcNow > EndDate;

    /// <summary>
    /// Activate the featured listing (after payment)
    /// </summary>
    public FeaturedListing Activate(Guid paymentId) =>
        this with
        {
            Status = FeaturedStatus.Active,
            PaymentId = Some(paymentId),
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Cancel the featured listing
    /// </summary>
    public FeaturedListing Cancel() =>
        this with
        {
            Status = FeaturedStatus.Canceled,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Mark as expired
    /// </summary>
    public FeaturedListing MarkExpired() =>
        this with
        {
            Status = FeaturedStatus.Expired,
            UpdatedAt = DateTime.UtcNow
        };

    /// <summary>
    /// Extend the duration of the featured listing
    /// </summary>
    public FeaturedListing ExtendDuration(int additionalDays, Guid newPaymentId) =>
        this with
        {
            EndDate = EndDate.AddDays(additionalDays),
            PaymentId = Some(newPaymentId),
            PriceGbp = PriceGbp + GetPriceForType(Type, additionalDays),
            UpdatedAt = DateTime.UtcNow
        };
}

/// <summary>
/// Type of featured placement
/// </summary>
public enum FeaturedType
{
    /// <summary>
    /// Homepage hero placement - £99/week
    /// </summary>
    HomepageHero,

    /// <summary>
    /// Category top placement - £49/week
    /// </summary>
    CategoryTop,

    /// <summary>
    /// Search result boost - £29/week
    /// </summary>
    SearchBoost,

    /// <summary>
    /// Spotlight badge - £15/listing
    /// </summary>
    Spotlight
}

/// <summary>
/// Featured listing status
/// </summary>
public enum FeaturedStatus
{
    Pending,
    Active,
    Expired,
    Canceled
}
