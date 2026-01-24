using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

public interface IListingRepository
{
    Task<Either<string, Seq<VehicleListing>>> GetActiveListingsAsync();
    Task<Option<VehicleListing>> GetByIdAsync(Guid id);
    Task<Either<string, Seq<VehicleListing>>> GetBySellerAsync(Guid sellerId);
    Task<Either<string, Seq<VehicleListing>>> SearchAsync(ListingSearchCriteria criteria);
    Task<Either<string, VehicleListing>> AddAsync(VehicleListing listing);
    Task<Either<string, Unit>> UpdateAsync(VehicleListing listing);
    Task<Either<string, Unit>> DeleteAsync(Guid id);
    Task<Either<string, Unit>> MarkAsSoldAsync(Guid id);
}

public class ListingRepository : IListingRepository
{
    private readonly EvMarketplaceDbContext _context;

    public ListingRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Either<string, Seq<VehicleListing>>> GetActiveListingsAsync()
    {
        try
        {
            var listings = await _context.VehicleListings
                .Where(l => l.Status == ListingStatus.Active)
                .OrderByDescending(l => l.ListedAt)
                .ToListAsync();

            return Seq(listings);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve listings: {ex.Message}";
        }
    }

    public async Task<Option<VehicleListing>> GetByIdAsync(Guid id)
    {
        try
        {
            var listing = await _context.VehicleListings.FindAsync(id);
            return Optional(listing);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seq<VehicleListing>>> GetBySellerAsync(Guid sellerId)
    {
        try
        {
            var listings = await _context.VehicleListings
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.ListedAt)
                .ToListAsync();

            return Seq(listings);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve seller listings: {ex.Message}";
        }
    }

    public async Task<Either<string, Seq<VehicleListing>>> SearchAsync(ListingSearchCriteria criteria)
    {
        try
        {
            var query = _context.VehicleListings
                .Where(l => l.Status == ListingStatus.Active);

            query = criteria.Make.Match(
                Some: make => query.Where(l => l.Make.ToLower().Contains(make.ToLower())),
                None: () => query);

            query = criteria.BodyType.Match(
                Some: bodyType => query.Where(l => l.BodyType.ToLower() == bodyType.ToLower()),
                None: () => query);

            query = criteria.Condition.Match(
                Some: condition => query.Where(l => l.Condition == condition),
                None: () => query);

            query = criteria.MinPrice.Match(
                Some: minPrice => query.Where(l => l.AskingPriceGbp >= minPrice),
                None: () => query);

            query = criteria.MaxPrice.Match(
                Some: maxPrice => query.Where(l => l.AskingPriceGbp <= maxPrice),
                None: () => query);

            query = criteria.MaxMileage.Match(
                Some: maxMileage => query.Where(l => l.Mileage <= maxMileage),
                None: () => query);

            query = criteria.Location.Match(
                Some: location => query.Where(l => l.Location.ToLower().Contains(location.ToLower())),
                None: () => query);

            var results = await query.OrderByDescending(l => l.ListedAt).ToListAsync();
            return Seq(results);
        }
        catch (Exception ex)
        {
            return $"Search failed: {ex.Message}";
        }
    }

    public async Task<Either<string, VehicleListing>> AddAsync(VehicleListing listing)
    {
        try
        {
            _context.VehicleListings.Add(listing);
            await _context.SaveChangesAsync();
            return listing;
        }
        catch (Exception ex)
        {
            return $"Failed to add listing: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> UpdateAsync(VehicleListing listing)
    {
        try
        {
            _context.VehicleListings.Update(listing);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to update listing: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> DeleteAsync(Guid id)
    {
        try
        {
            var listing = await _context.VehicleListings.FindAsync(id);
            if (listing is null)
                return "Listing not found";

            _context.VehicleListings.Remove(listing);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to delete listing: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> MarkAsSoldAsync(Guid id)
    {
        try
        {
            var listing = await _context.VehicleListings.FindAsync(id);
            if (listing is null)
                return "Listing not found";

            var updated = listing with
            {
                Status = ListingStatus.Sold,
                SoldAt = Some(DateTime.UtcNow)
            };

            _context.VehicleListings.Update(updated);
            await _context.SaveChangesAsync();
            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to mark listing as sold: {ex.Message}";
        }
    }
}

public sealed record ListingSearchCriteria(
    Option<string> Make,
    Option<string> BodyType,
    Option<VehicleCondition> Condition,
    Option<decimal> MinPrice,
    Option<decimal> MaxPrice,
    Option<decimal> MaxMileage,
    Option<string> Location
);
