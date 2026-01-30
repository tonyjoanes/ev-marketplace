using EvMarketplace.Domain.Models;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;

namespace EvMarketplace.Tests.Models;

public class SellerTests
{
    [Fact]
    public void Create_ValidPrivateSeller_ReturnsSeller()
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            "John Doe",
            SellerType.Private,
            "john@example.com",
            "+44 7700 900123",
            "hashedpassword123",
            None,
            Some("London, UK"));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: seller =>
            {
                seller.Name.Should().Be("John Doe");
                seller.Type.Should().Be(SellerType.Private);
                seller.Email.Should().Be("john@example.com");
                seller.CompanyName.IsNone.Should().BeTrue();
                seller.CurrentSubscriptionTier.Should().Be(SubscriptionTier.Free);
                seller.CurrentListingCount.Should().Be(0);
                seller.StripeCustomerId.Should().Be("");
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Fact]
    public void Create_ValidDealer_ReturnsSeller()
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            "Bob Smith",
            SellerType.Dealer,
            "bob@dealership.com",
            "+44 7700 900456",
            "hashedpassword456",
            Some("Premium Motors Ltd"),
            Some("Manchester, UK"));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: seller =>
            {
                seller.Type.Should().Be(SellerType.Dealer);
                seller.CompanyName.IsSome.Should().BeTrue();
                seller.CompanyName.Match(name => name.Should().Be("Premium Motors Ltd"), () => { });
            },
            Left: error => throw new Exception($"Expected success but got error: {error}")
        );
    }

    [Theory]
    [InlineData("", "john@example.com", "Name cannot be empty")]
    [InlineData("   ", "john@example.com", "Name cannot be empty")]
    [InlineData(null, "john@example.com", "Name cannot be empty")]
    public void Create_InvalidName_ReturnsError(string? name, string email, string expectedError)
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            name ?? "",
            SellerType.Private,
            email,
            "+44 7700 900123",
            "hashedpassword");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain(expectedError));
    }

    [Theory]
    [InlineData("", "Invalid")]
    [InlineData("notanemail", "Invalid")]
    [InlineData("   ", "Invalid")]
    public void Create_InvalidEmail_ReturnsError(string email, string expectedErrorPart)
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            "John Doe",
            SellerType.Private,
            email,
            "+44 7700 900123",
            "hashedpassword");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("email"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_InvalidPhoneNumber_ReturnsError(string phone)
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            "John Doe",
            SellerType.Private,
            "john@example.com",
            phone,
            "hashedpassword");

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Phone number"));
    }

    [Fact]
    public void Create_DealerWithoutCompanyName_ReturnsError()
    {
        // Act
        var result = Seller.Create(
            Guid.NewGuid(),
            "Bob Smith",
            SellerType.Dealer,
            "bob@dealership.com",
            "+44 7700 900456",
            "hashedpassword",
            None); // Missing company name

        // Assert
        result.IsLeft.Should().BeTrue();
        result.IfLeft(error => error.Should().Contain("Company name is required for dealers"));
    }

    [Fact]
    public void CanAddListing_FreeTierWithNoListings_ReturnsTrue()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Free, currentListingCount: 0);

        // Act
        var canAdd = seller.CanAddListing();

        // Assert
        canAdd.Should().BeTrue("Free tier allows 1 listing");
    }

    [Fact]
    public void CanAddListing_FreeTierWithOneListings_ReturnsFalse()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Free, currentListingCount: 1);

        // Act
        var canAdd = seller.CanAddListing();

        // Assert
        canAdd.Should().BeFalse("Free tier limit is 1 listing");
    }

    [Fact]
    public void CanAddListing_ProTierWithNineListings_ReturnsTrue()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 9);

        // Act
        var canAdd = seller.CanAddListing();

        // Assert
        canAdd.Should().BeTrue("Pro tier allows 10 listings");
    }

    [Fact]
    public void CanAddListing_ProTierWithTenListings_ReturnsFalse()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 10);

        // Act
        var canAdd = seller.CanAddListing();

        // Assert
        canAdd.Should().BeFalse("Pro tier limit is 10 listings");
    }

    [Fact]
    public void CanAddListing_EnterpriseTierWithManyListings_ReturnsTrue()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Enterprise, currentListingCount: 1000);

        // Act
        var canAdd = seller.CanAddListing();

        // Assert
        canAdd.Should().BeTrue("Enterprise tier has unlimited listings");
    }

    [Fact]
    public void GetRemainingListings_FreeTierWithNoListings_ReturnsOne()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Free, currentListingCount: 0);

        // Act
        var remaining = seller.GetRemainingListings();

        // Assert
        remaining.Should().Be(1);
    }

    [Fact]
    public void GetRemainingListings_ProTierWithFiveListings_ReturnsFive()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 5);

        // Act
        var remaining = seller.GetRemainingListings();

        // Assert
        remaining.Should().Be(5, "Pro tier allows 10 listings, 5 used = 5 remaining");
    }

    [Fact]
    public void GetRemainingListings_EnterpriseTier_ReturnsMaxValue()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Enterprise, currentListingCount: 100);

        // Act
        var remaining = seller.GetRemainingListings();

        // Assert
        remaining.Should().Be(int.MaxValue, "Enterprise tier has unlimited listings");
    }

    [Fact]
    public void IncrementListingCount_IncrementsCorrectly()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 5);

        // Act
        var updatedSeller = seller.IncrementListingCount();

        // Assert
        updatedSeller.CurrentListingCount.Should().Be(6);
        seller.CurrentListingCount.Should().Be(5, "Original should be unchanged (immutability)");
    }

    [Fact]
    public void DecrementListingCount_DecrementsCorrectly()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 5);

        // Act
        var updatedSeller = seller.DecrementListingCount();

        // Assert
        updatedSeller.CurrentListingCount.Should().Be(4);
        seller.CurrentListingCount.Should().Be(5, "Original should be unchanged (immutability)");
    }

    [Fact]
    public void DecrementListingCount_AtZero_StaysAtZero()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Pro, currentListingCount: 0);

        // Act
        var updatedSeller = seller.DecrementListingCount();

        // Assert
        updatedSeller.CurrentListingCount.Should().Be(0, "Should not go below zero");
    }

    [Fact]
    public void UpdateSubscription_UpdatesTierAndIds()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Free, currentListingCount: 0);
        var subscriptionId = Guid.NewGuid();
        var stripeCustomerId = "cus_123456";

        // Act
        var updatedSeller = seller.UpdateSubscription(SubscriptionTier.Pro, subscriptionId, stripeCustomerId);

        // Assert
        updatedSeller.CurrentSubscriptionTier.Should().Be(SubscriptionTier.Pro);
        updatedSeller.ActiveSubscriptionId.IsSome.Should().BeTrue();
        updatedSeller.ActiveSubscriptionId.Match(
            id => id.Should().Be(subscriptionId),
            () => throw new Exception("Expected subscription ID")
        );
        updatedSeller.StripeCustomerId.Should().Be(stripeCustomerId);

        // Original should be unchanged
        seller.CurrentSubscriptionTier.Should().Be(SubscriptionTier.Free);
    }

    [Fact]
    public void CancelSubscription_RevertsToFreeTier()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Premium, currentListingCount: 10);
        var updatedSeller = seller.UpdateSubscription(
            SubscriptionTier.Premium,
            Guid.NewGuid(),
            "cus_123456");

        // Act
        var cancelledSeller = updatedSeller.CancelSubscription();

        // Assert
        cancelledSeller.CurrentSubscriptionTier.Should().Be(SubscriptionTier.Free);
        cancelledSeller.ActiveSubscriptionId.IsNone.Should().BeTrue();

        // StripeCustomerId should remain (for history)
        cancelledSeller.StripeCustomerId.Should().Be("cus_123456");
    }

    [Fact]
    public void Seller_IsImmutable()
    {
        // Arrange
        var seller = CreateValidSeller(SubscriptionTier.Free, currentListingCount: 0);

        // Act - create modified copy using with expression
        var modified = seller with { Name = "Modified Name" };

        // Assert
        seller.Name.Should().Be("John Doe", "Original should not be modified");
        modified.Name.Should().Be("Modified Name", "Modified copy should have new name");
        seller.Id.Should().Be(modified.Id, "Other properties should be the same");
    }

    // Helper method to create valid sellers for testing
    private Seller CreateValidSeller(
        SubscriptionTier tier = SubscriptionTier.Free,
        int currentListingCount = 0)
    {
        var result = Seller.Create(
            Guid.NewGuid(),
            "John Doe",
            SellerType.Private,
            "john@example.com",
            "+44 7700 900123",
            "hashedpassword",
            None,
            Some("London, UK"),
            subscriptionTier: tier);

        var seller = result.IfLeft(err => throw new Exception($"Failed to create test seller: {err}"));

        // Manually set listing count if needed
        if (currentListingCount > 0)
        {
            seller = seller with { CurrentListingCount = currentListingCount };
        }

        return seller;
    }
}
