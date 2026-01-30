using EvMarketplace.Domain.Models;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Tests.Models;

public class PaymentTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsPayment()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        // Act
        var result = Payment.Create(
            id,
            sellerId,
            PaymentType.Subscription,
            49.99m,
            "pi_123456789",
            "Monthly Pro subscription",
            Some(subscriptionId),
            None);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: payment =>
            {
                payment.Id.Should().Be(id);
                payment.SellerId.Should().Be(sellerId);
                payment.Type.Should().Be(PaymentType.Subscription);
                payment.AmountGbp.Should().Be(49.99m);
                payment.Status.Should().Be(PaymentStatus.Pending);
                payment.StripePaymentIntentId.Should().Be("pi_123456789");
                payment.Description.Should().Be("Monthly Pro subscription");
                payment.SubscriptionId.IsSome.Should().BeTrue();
                payment.FeaturedListingId.IsNone.Should().BeTrue();
                payment.PaidAt.IsNone.Should().BeTrue();
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void Create_FeaturedListingPayment_SetsCorrectFields()
    {
        // Arrange
        var featuredListingId = Guid.NewGuid();

        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.FeaturedListing,
            99.00m,
            "pi_987654321",
            "Homepage Hero - 1 week",
            None,
            Some(featuredListingId));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: payment =>
            {
                payment.Type.Should().Be(PaymentType.FeaturedListing);
                payment.FeaturedListingId.IsSome.Should().BeTrue();
                payment.SubscriptionId.IsNone.Should().BeTrue();
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void Create_EmptyId_ReturnsError()
    {
        // Act
        var result = Payment.Create(
            Guid.Empty,
            Guid.NewGuid(),
            PaymentType.Subscription,
            49.99m,
            "pi_123456789",
            "Test payment");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Payment ID cannot be empty"));
    }

    [Fact]
    public void Create_EmptySellerId_ReturnsError()
    {
        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.Empty,
            PaymentType.Subscription,
            49.99m,
            "pi_123456789",
            "Test payment");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Seller ID cannot be empty"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public void Create_InvalidAmount_ReturnsError(decimal invalidAmount)
    {
        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.Subscription,
            invalidAmount,
            "pi_123456789",
            "Test payment");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Payment amount must be greater than zero"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidStripePaymentIntentId_ReturnsError(string invalidIntentId)
    {
        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.Subscription,
            49.99m,
            invalidIntentId,
            "Test payment");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Stripe payment intent ID is required"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidDescription_ReturnsError(string invalidDescription)
    {
        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.Subscription,
            49.99m,
            "pi_123456789",
            invalidDescription);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Description is required"));
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsError()
    {
        // Arrange
        var longDescription = new string('A', 501); // More than 500 characters

        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentType.Subscription,
            49.99m,
            "pi_123456789",
            longDescription);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("less than 500 characters"));
    }

    [Fact]
    public void MarkAsSucceeded_UpdatesStatusAndPaidAt()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Pending);

        // Act
        var succeeded = payment.MarkAsSucceeded();

        // Assert
        succeeded.Status.Should().Be(PaymentStatus.Succeeded);
        succeeded.PaidAt.IsSome.Should().BeTrue();
        succeeded.PaidAt.Match(
            paidAt => paidAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)),
            () => throw new Exception("Expected paid at date")
        );
        succeeded.UpdatedAt.Should().BeAfter(payment.UpdatedAt);

        // Original should be unchanged
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.PaidAt.IsNone.Should().BeTrue();
    }

    [Fact]
    public void MarkAsFailed_UpdatesStatusOnly()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Processing);

        // Act
        var failed = payment.MarkAsFailed();

        // Assert
        failed.Status.Should().Be(PaymentStatus.Failed);
        failed.PaidAt.IsNone.Should().BeTrue("Failed payments should not have paid date");
        failed.UpdatedAt.Should().BeAfter(payment.UpdatedAt);

        // Original should be unchanged
        payment.Status.Should().Be(PaymentStatus.Processing);
    }

    [Fact]
    public void MarkAsRefunded_UpdatesStatus()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Succeeded)
            .MarkAsSucceeded();

        // Act
        var refunded = payment.MarkAsRefunded();

        // Assert
        refunded.Status.Should().Be(PaymentStatus.Refunded);
        refunded.UpdatedAt.Should().BeAfter(payment.UpdatedAt);

        // Original should be unchanged
        payment.Status.Should().Be(PaymentStatus.Succeeded);
    }

    [Fact]
    public void IsSuccessful_SucceededStatus_ReturnsTrue()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Succeeded);

        // Act
        var isSuccessful = payment.IsSuccessful();

        // Assert
        isSuccessful.Should().BeTrue();
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Canceled)]
    [InlineData(PaymentStatus.Refunded)]
    public void IsSuccessful_NonSucceededStatus_ReturnsFalse(PaymentStatus status)
    {
        // Arrange
        var payment = CreateValidPayment(status: status);

        // Act
        var isSuccessful = payment.IsSuccessful();

        // Assert
        isSuccessful.Should().BeFalse();
    }

    [Fact]
    public void Payment_StatusTransition_PendingToSucceeded()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Pending);

        // Act
        var processing = payment with { Status = PaymentStatus.Processing };
        var succeeded = processing.MarkAsSucceeded();

        // Assert - typical payment flow
        payment.Status.Should().Be(PaymentStatus.Pending);
        processing.Status.Should().Be(PaymentStatus.Processing);
        succeeded.Status.Should().Be(PaymentStatus.Succeeded);
        succeeded.PaidAt.IsSome.Should().BeTrue();
    }

    [Fact]
    public void Payment_StatusTransition_SucceededToRefunded()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Pending);
        var succeeded = payment.MarkAsSucceeded();

        // Act
        var refunded = succeeded.MarkAsRefunded();

        // Assert - refund flow
        succeeded.Status.Should().Be(PaymentStatus.Succeeded);
        refunded.Status.Should().Be(PaymentStatus.Refunded);
        refunded.PaidAt.IsSome.Should().BeTrue("Paid date should remain after refund");
    }

    [Fact]
    public void Payment_IsImmutable()
    {
        // Arrange
        var payment = CreateValidPayment(status: PaymentStatus.Pending);

        // Act - create modified copy using with expression
        var modified = payment with { AmountGbp = 99.99m };

        // Assert
        payment.AmountGbp.Should().Be(49.99m, "Original should not be modified");
        modified.AmountGbp.Should().Be(99.99m, "Modified copy should have new amount");
        payment.Id.Should().Be(modified.Id, "Other properties should be the same");
    }

    [Theory]
    [InlineData(PaymentType.Subscription)]
    [InlineData(PaymentType.FeaturedListing)]
    [InlineData(PaymentType.SingleListing)]
    [InlineData(PaymentType.LeadPurchase)]
    public void Create_AllPaymentTypes_Succeed(PaymentType paymentType)
    {
        // Act
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            paymentType,
            49.99m,
            "pi_123456789",
            $"Test {paymentType} payment");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: payment => payment.Type.Should().Be(paymentType),
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    // Helper method to create valid payments for testing
    private Payment CreateValidPayment(
        PaymentStatus status = PaymentStatus.Pending,
        PaymentType type = PaymentType.Subscription,
        decimal amount = 49.99m)
    {
        var result = Payment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            amount,
            "pi_123456789",
            "Test payment",
            Some(Guid.NewGuid()),
            None);

        var payment = result.IfLeft(err => throw new Exception($"Failed to create test payment: {err}"));

        // Set status if not Pending
        if (status != PaymentStatus.Pending)
        {
            payment = payment with { Status = status };
        }

        return payment;
    }
}
