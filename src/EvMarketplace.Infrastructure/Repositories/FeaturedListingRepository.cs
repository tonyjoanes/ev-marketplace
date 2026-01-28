using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

public interface IFeaturedListingRepository
{
    Task<Option<FeaturedListing>> GetByIdAsync(Guid id);
    Task<Option<FeaturedListing>> GetByListingIdAsync(Guid listingId);
    Task<Either<string, Seq<FeaturedListing>>> GetActiveByTypeAsync(FeaturedType type);
    Task<Either<string, Seq<FeaturedListing>>> GetBySellerIdAsync(Guid sellerId);
    Task<Either<string, Seq<FeaturedListing>>> GetExpiredAsync();
    Task<Either<string, FeaturedListing>> AddAsync(FeaturedListing featuredListing);
    Task<Either<string, FeaturedListing>> UpdateAsync(FeaturedListing featuredListing);
    Task<Either<string, Unit>> DeleteAsync(Guid id);
}

public class FeaturedListingRepository : IFeaturedListingRepository
{
    private readonly EvMarketplaceDbContext _context;

    public FeaturedListingRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Option<FeaturedListing>> GetByIdAsync(Guid id)
    {
        try
        {
            var featured = await _context.FeaturedListings
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            return Optional(featured);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<FeaturedListing>> GetByListingIdAsync(Guid listingId)
    {
        try
        {
            var featured = await _context.FeaturedListings
                .AsNoTracking()
                .Where(f => f.ListingId == listingId && f.Status == FeaturedStatus.Active)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();

            return Optional(featured);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seq<FeaturedListing>>> GetActiveByTypeAsync(FeaturedType type)
    {
        try
        {
            var now = DateTime.UtcNow;
            var featured = await _context.FeaturedListings
                .AsNoTracking()
                .Where(f => f.Type == type &&
                           f.Status == FeaturedStatus.Active &&
                           f.StartDate <= now &&
                           f.EndDate >= now)
                .OrderBy(f => f.StartDate) // First-in-first-out for featured rotation
                .ToListAsync();

            return toSeq(featured);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve active featured listings: {ex.Message}";
        }
    }

    public async Task<Either<string, Seq<FeaturedListing>>> GetBySellerIdAsync(Guid sellerId)
    {
        try
        {
            var featured = await _context.FeaturedListings
                .AsNoTracking()
                .Where(f => f.SellerId == sellerId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return toSeq(featured);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve featured listings for seller: {ex.Message}";
        }
    }

    public async Task<Either<string, Seq<FeaturedListing>>> GetExpiredAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var expired = await _context.FeaturedListings
                .Where(f => f.Status == FeaturedStatus.Active && f.EndDate < now)
                .ToListAsync();

            return toSeq(expired);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve expired featured listings: {ex.Message}";
        }
    }

    public async Task<Either<string, FeaturedListing>> AddAsync(FeaturedListing featuredListing)
    {
        try
        {
            await _context.FeaturedListings.AddAsync(featuredListing);
            await _context.SaveChangesAsync();
            return featuredListing;
        }
        catch (Exception ex)
        {
            return $"Failed to add featured listing: {ex.Message}";
        }
    }

    public async Task<Either<string, FeaturedListing>> UpdateAsync(FeaturedListing featuredListing)
    {
        try
        {
            var existing = await _context.FeaturedListings.FindAsync(featuredListing.Id);

            if (existing == null)
                return "Featured listing not found";

            _context.Entry(existing).CurrentValues.SetValues(featuredListing);
            await _context.SaveChangesAsync();

            return featuredListing;
        }
        catch (Exception ex)
        {
            return $"Failed to update featured listing: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> DeleteAsync(Guid id)
    {
        try
        {
            var featured = await _context.FeaturedListings.FindAsync(id);

            if (featured == null)
                return "Featured listing not found";

            _context.FeaturedListings.Remove(featured);
            await _context.SaveChangesAsync();

            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to delete featured listing: {ex.Message}";
        }
    }
}
