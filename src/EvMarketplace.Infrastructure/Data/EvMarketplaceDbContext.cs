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
    }
}
