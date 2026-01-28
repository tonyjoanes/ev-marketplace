using EvMarketplace.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EvMarketplace.Infrastructure.Data;

public class EvMarketplaceDbContext : DbContext
{
    public EvMarketplaceDbContext(DbContextOptions<EvMarketplaceDbContext> options)
        : base(options)
    {
    }

    public DbSet<ElectricVehicle> ElectricVehicles => Set<ElectricVehicle>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<VehicleListing> VehicleListings => Set<VehicleListing>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FeaturedListing> FeaturedListings => Set<FeaturedListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ElectricVehicle>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Make).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BodyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BatteryCapacityKwh).HasPrecision(10, 2);
            entity.Property(e => e.WltpRangeKm).HasPrecision(10, 2);
            entity.Property(e => e.EfficiencyKwhPer100Km).HasPrecision(10, 2);
            entity.Property(e => e.AcChargeRateKw).HasPrecision(10, 2);
            entity.Property(e => e.DcChargeRateKw).HasPrecision(10, 2);
            entity.Property(e => e.PriceGbp).HasPrecision(10, 2);

            // Store connector types as JSON
            entity.Property(e => e.ConnectorTypes)
                .HasConversion(
                    v => string.Join(',', v),
                    v => LanguageExt.Prelude.Seq(v.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                .HasMaxLength(200);

            entity.HasIndex(e => e.Make);
            entity.HasIndex(e => e.BodyType);
            entity.HasIndex(e => e.PriceGbp);
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CurrentSubscriptionTier).IsRequired();
            entity.Property(e => e.StripeCustomerId).HasMaxLength(100);
            entity.Property(e => e.CurrentListingCount).IsRequired();

            // Handle Option<string> types
            entity.Property(e => e.CompanyName)
                .HasConversion(
                    v => v.IsSome ? v.Match(s => s, () => null) : null,
                    v => v != null ? LanguageExt.Prelude.Some(v) : LanguageExt.Prelude.None)
                .HasMaxLength(200);

            entity.Property(e => e.Location)
                .HasConversion(
                    v => v.IsSome ? v.Match(s => s, () => null) : null,
                    v => v != null ? LanguageExt.Prelude.Some(v) : LanguageExt.Prelude.None)
                .HasMaxLength(200);

            // Handle Option<Guid> for ActiveSubscriptionId
            entity.Property(e => e.ActiveSubscriptionId)
                .HasConversion(
                    v => v.IsSome ? v.Match(g => (Guid?)g, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.StripeCustomerId);
        });

        modelBuilder.Entity<VehicleListing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Make).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BodyType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);

            entity.Property(e => e.BatteryCapacityKwh).HasPrecision(10, 2);
            entity.Property(e => e.WltpRangeKm).HasPrecision(10, 2);
            entity.Property(e => e.EfficiencyKwhPer100Km).HasPrecision(10, 2);
            entity.Property(e => e.Mileage).HasPrecision(10, 2);
            entity.Property(e => e.AskingPriceGbp).HasPrecision(10, 2);

            entity.Property(e => e.Condition).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            // Store image URLs as comma-separated
            entity.Property(e => e.ImageUrls)
                .HasConversion(
                    v => string.Join(',', v),
                    v => LanguageExt.Prelude.Seq(v.Split(',', StringSplitOptions.RemoveEmptyEntries)))
                .HasMaxLength(2000);

            // Handle Option<Guid> for catalogue reference
            entity.Property(e => e.CatalogueVehicleId)
                .HasConversion(
                    v => v.IsSome ? v.Match(g => (Guid?)g, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Handle Option<DateTime> for SoldAt
            entity.Property(e => e.SoldAt)
                .HasConversion(
                    v => v.IsSome ? v.Match(d => (DateTime?)d, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Relationship with Seller
            entity.HasOne<Seller>()
                .WithMany()
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional relationship with ElectricVehicle catalogue
            entity.HasOne<ElectricVehicle>()
                .WithMany()
                .HasForeignKey("CatalogueVehicleId")
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Make);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Condition);
            entity.HasIndex(e => e.AskingPriceGbp);
            entity.HasIndex(e => e.ListedAt);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Tier).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(100);
            entity.Property(e => e.StripeCustomerId).IsRequired().HasMaxLength(100);

            // Handle Option<DateTime> types
            entity.Property(e => e.EndDate)
                .HasConversion(
                    v => v.IsSome ? v.Match(d => (DateTime?)d, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            entity.Property(e => e.TrialEndDate)
                .HasConversion(
                    v => v.IsSome ? v.Match(d => (DateTime?)d, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Relationship with Seller
            entity.HasOne<Seller>()
                .WithMany()
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.StripeSubscriptionId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CurrentPeriodEnd);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.AmountGbp).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.StripePaymentIntentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);

            // Handle Option<Guid> types
            entity.Property(e => e.SubscriptionId)
                .HasConversion(
                    v => v.IsSome ? v.Match(g => (Guid?)g, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            entity.Property(e => e.FeaturedListingId)
                .HasConversion(
                    v => v.IsSome ? v.Match(g => (Guid?)g, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Handle Option<DateTime>
            entity.Property(e => e.PaidAt)
                .HasConversion(
                    v => v.IsSome ? v.Match(d => (DateTime?)d, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Relationship with Seller
            entity.HasOne<Seller>()
                .WithMany()
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.StripePaymentIntentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<FeaturedListing>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ListingId).IsRequired();
            entity.Property(e => e.SellerId).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.PriceGbp).HasPrecision(10, 2).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            // Handle Option<Guid>
            entity.Property(e => e.PaymentId)
                .HasConversion(
                    v => v.IsSome ? v.Match(g => (Guid?)g, () => null) : null,
                    v => v.HasValue ? LanguageExt.Prelude.Some(v.Value) : LanguageExt.Prelude.None);

            // Relationship with VehicleListing
            entity.HasOne<VehicleListing>()
                .WithMany()
                .HasForeignKey(e => e.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relationship with Seller
            entity.HasOne<Seller>()
                .WithMany()
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ListingId);
            entity.HasIndex(e => e.SellerId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.EndDate);
        });
    }
}
