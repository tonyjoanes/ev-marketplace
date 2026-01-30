using EvMarketplace.Domain.Models;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Tests.Models;

public class SubscriptionTests
{
    [Fact]
    public void Create_ValidInputs_ReturnsSubscription()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddMonths(1);

        // Act
        var result = Subscription.Create(
            id,
            sellerId,
            SubscriptionTier.Pro,
            "sub_123456",
            "cus_123456",
            periodStart,
            periodEnd);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: subscription =>
            {
                subscription.Id.Should().Be(id);
                subscription.SellerId.Should().Be(sellerId);
                subscription.Tier.Should().Be(SubscriptionTier.Pro);
                subscription.Status.Should().Be(SubscriptionStatus.Active);
                subscription.StripeSubscriptionId.Should().Be("sub_123456");
                subscription.StripeCustomerId.Should().Be("cus_123456");
                subscription.CurrentPeriodStart.Should().Be(periodStart);
                subscription.CurrentPeriodEnd.Should().Be(periodEnd);
                subscription.CancelAtPeriodEnd.Should().BeFalse();
                subscription.EndDate.IsNone.Should().BeTrue();
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void Create_WithTrialPeriod_SetsTrialEndDate()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddMonths(1);
        var trialEnd = periodStart.AddDays(14);

        // Act
        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            "sub_123456",
            "cus_123456",
            periodStart,
            periodEnd,
            Some(trialEnd));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: subscription =>
            {
                subscription.TrialEndDate.IsSome.Should().BeTrue();
                subscription.TrialEndDate.Match(
                    trial => trial.Should().Be(trialEnd),
                    () => throw new Exception("Expected trial end date")
                );
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void Create_EmptyId_ReturnsError()
    {
        // Act
        var result = Subscription.Create(
            Guid.Empty,
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            "sub_123456",
            "cus_123456",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Subscription ID cannot be empty"));
    }

    [Fact]
    public void Create_EmptySellerId_ReturnsError()
    {
        // Act
        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.Empty,
            SubscriptionTier.Pro,
            "sub_123456",
            "cus_123456",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Seller ID cannot be empty"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidStripeSubscriptionId_ReturnsError(string stripeSubId)
    {
        // Act
        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            stripeSubId,
            "cus_123456",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Stripe subscription ID"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidStripeCustomerId_ReturnsError(string stripeCusId)
    {
        // Act
        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            "sub_123456",
            stripeCusId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1));

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Stripe customer ID"));
    }

    [Fact]
    public void Create_EndBeforeStart_ReturnsError()
    {
        // Arrange
        var periodStart = DateTime.UtcNow;
        var periodEnd = periodStart.AddDays(-1); // End before start

        // Act
        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubscriptionTier.Pro,
            "sub_123456",
            "cus_123456",
            periodStart,
            periodEnd);

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Period end must be after period start"));
    }

    [Fact]
    public void CreateFree_ValidInputs_ReturnsSubscription()
    {
        // Arrange
        var id = Guid.NewGuid();
        var sellerId = Guid.NewGuid();

        // Act
        var result = Subscription.CreateFree(id, sellerId);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: subscription =>
            {
                subscription.Id.Should().Be(id);
                subscription.SellerId.Should().Be(sellerId);
                subscription.Tier.Should().Be(SubscriptionTier.Free);
                subscription.Status.Should().Be(SubscriptionStatus.Active);
                subscription.StripeSubscriptionId.Should().BeEmpty();
                subscription.StripeCustomerId.Should().BeEmpty();
                subscription.CancelAtPeriodEnd.Should().BeFalse();
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void IsActive_ActiveWithinPeriod_ReturnsTrue()
    {
        // Arrange
        var subscription = CreateValidSubscription(
            status: SubscriptionStatus.Active,
            periodEnd: DateTime.UtcNow.AddDays(7));

        // Act
        var isActive = subscription.IsActive();

        // Assert
        isActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ActiveButExpired_ReturnsFalse()
    {
        // Arrange
        var subscription = CreateValidSubscription(
            status: SubscriptionStatus.Active,
            periodEnd: DateTime.UtcNow.AddDays(-1)); // Expired

        // Act
        var isActive = subscription.IsActive();

        // Assert
        isActive.Should().BeFalse("Subscription period has ended");
    }

    [Fact]
    public void IsActive_CanceledStatus_ReturnsFalse()
    {
        // Arrange
        var subscription = CreateValidSubscription(
            status: SubscriptionStatus.Canceled,
            periodEnd: DateTime.UtcNow.AddDays(7));

        // Act
        var isActive = subscription.IsActive();

        // Assert
        isActive.Should().BeFalse("Status is Canceled");
    }

    [Fact]
    public void IsInTrial_WithinTrialPeriod_ReturnsTrue()
    {
        // Arrange
        var trialEnd = DateTime.UtcNow.AddDays(7);
        var subscription = CreateValidSubscription(trialEndDate: Some(trialEnd));

        // Act
        var isInTrial = subscription.IsInTrial();

        // Assert
        isInTrial.Should().BeTrue();
    }

    [Fact]
    public void IsInTrial_TrialExpired_ReturnsFalse()
    {
        // Arrange
        var trialEnd = DateTime.UtcNow.AddDays(-1); // Expired
        var subscription = CreateValidSubscription(trialEndDate: Some(trialEnd));

        // Act
        var isInTrial = subscription.IsInTrial();

        // Assert
        isInTrial.Should().BeFalse("Trial period has ended");
    }

    [Fact]
    public void IsInTrial_NoTrialPeriod_ReturnsFalse()
    {
        // Arrange
        var subscription = CreateValidSubscription(trialEndDate: None);

        // Act
        var isInTrial = subscription.IsInTrial();

        // Assert
        isInTrial.Should().BeFalse("No trial period set");
    }

    [Fact]
    public void ScheduleCancellation_SetsCancelFlag()
    {
        // Arrange
        var subscription = CreateValidSubscription();

        // Act
        var cancelled = subscription.ScheduleCancellation();

        // Assert
        cancelled.CancelAtPeriodEnd.Should().BeTrue();
        cancelled.UpdatedAt.Should().BeAfter(subscription.UpdatedAt);

        // Original should be unchanged
        subscription.CancelAtPeriodEnd.Should().BeFalse();
    }

    [Fact]
    public void UndoCancellation_ClearsCancelFlag()
    {
        // Arrange
        var subscription = CreateValidSubscription();
        var cancelled = subscription.ScheduleCancellation();

        // Act
        var reactivated = cancelled.UndoCancellation();

        // Assert
        reactivated.CancelAtPeriodEnd.Should().BeFalse();
        reactivated.UpdatedAt.Should().BeAfter(cancelled.UpdatedAt);
    }

    [Fact]
    public void ChangeTier_UpdatesTier()
    {
        // Arrange
        var subscription = CreateValidSubscription(tier: SubscriptionTier.Pro);

        // Act
        var upgraded = subscription.ChangeTier(SubscriptionTier.Premium);

        // Assert
        upgraded.Tier.Should().Be(SubscriptionTier.Premium);
        upgraded.UpdatedAt.Should().BeAfter(subscription.UpdatedAt);

        // Original should be unchanged
        subscription.Tier.Should().Be(SubscriptionTier.Pro);
    }

    [Fact]
    public void UpdateStatus_UpdatesStatus()
    {
        // Arrange
        var subscription = CreateValidSubscription(status: SubscriptionStatus.Active);

        // Act
        var updated = subscription.UpdateStatus(SubscriptionStatus.PastDue);

        // Assert
        updated.Status.Should().Be(SubscriptionStatus.PastDue);
        updated.UpdatedAt.Should().BeAfter(subscription.UpdatedAt);

        // Original should be unchanged
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Renew_UpdatesPeriodAndStatus()
    {
        // Arrange
        var subscription = CreateValidSubscription(
            status: SubscriptionStatus.Active,
            cancelAtPeriodEnd: true);

        var newStart = DateTime.UtcNow;
        var newEnd = newStart.AddMonths(1);

        // Act
        var renewed = subscription.Renew(newStart, newEnd);

        // Assert
        renewed.CurrentPeriodStart.Should().Be(newStart);
        renewed.CurrentPeriodEnd.Should().Be(newEnd);
        renewed.Status.Should().Be(SubscriptionStatus.Active);
        renewed.CancelAtPeriodEnd.Should().BeFalse("Renewal should clear cancel flag");
        renewed.UpdatedAt.Should().BeAfter(subscription.UpdatedAt);
    }

    [Fact]
    public void GetPlan_ReturnsPlanForTier()
    {
        // Arrange
        var subscription = CreateValidSubscription(tier: SubscriptionTier.Pro);

        // Act
        var planOption = subscription.GetPlan();

        // Assert
        planOption.IsSome.Should().BeTrue();
        planOption.Match(
            plan =>
            {
                plan.Tier.Should().Be(SubscriptionTier.Pro);
                plan.PriceGbpPerMonth.Should().Be(49m);
            },
            () => throw new Exception("Expected plan")
        );
    }

    [Fact]
    public void Subscription_IsImmutable()
    {
        // Arrange
        var subscription = CreateValidSubscription(tier: SubscriptionTier.Pro);

        // Act - create modified copy using with expression
        var modified = subscription with { Tier = SubscriptionTier.Premium };

        // Assert
        subscription.Tier.Should().Be(SubscriptionTier.Pro, "Original should not be modified");
        modified.Tier.Should().Be(SubscriptionTier.Premium, "Modified copy should have new tier");
        subscription.Id.Should().Be(modified.Id, "Other properties should be the same");
    }

    // Helper method to create valid subscriptions for testing
    private Subscription CreateValidSubscription(
        SubscriptionTier tier = SubscriptionTier.Pro,
        SubscriptionStatus status = SubscriptionStatus.Active,
        DateTime? periodEnd = null,
        bool cancelAtPeriodEnd = false,
        Option<DateTime> trialEndDate = default)
    {
        var periodStart = DateTime.UtcNow;
        var actualPeriodEnd = periodEnd ?? periodStart.AddMonths(1);

        var result = Subscription.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tier,
            "sub_123456",
            "cus_123456",
            periodStart,
            actualPeriodEnd,
            trialEndDate);

        var subscription = result.IfLeft(err => throw new Exception($"Failed to create test subscription: {err}"));

        // Apply additional properties if needed
        if (status != SubscriptionStatus.Active)
        {
            subscription = subscription.UpdateStatus(status);
        }

        if (cancelAtPeriodEnd)
        {
            subscription = subscription.ScheduleCancellation();
        }

        return subscription;
    }
}
