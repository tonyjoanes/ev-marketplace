using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

public interface ISubscriptionRepository
{
    Task<Option<Subscription>> GetByIdAsync(Guid id);
    Task<Option<Subscription>> GetBySellerIdAsync(Guid sellerId);
    Task<Option<Subscription>> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<Either<string, Seq<Subscription>>> GetExpiringSubscriptionsAsync(DateTime beforeDate);
    Task<Either<string, Subscription>> AddAsync(Subscription subscription);
    Task<Either<string, Subscription>> UpdateAsync(Subscription subscription);
    Task<Either<string, Unit>> DeleteAsync(Guid id);
}

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly EvMarketplaceDbContext _context;

    public SubscriptionRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Option<Subscription>> GetByIdAsync(Guid id)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            return Optional(subscription);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<Subscription>> GetBySellerIdAsync(Guid sellerId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.SellerId == sellerId && s.Status == SubscriptionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            return Optional(subscription);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<Subscription>> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        try
        {
            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

            return Optional(subscription);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seq<Subscription>>> GetExpiringSubscriptionsAsync(DateTime beforeDate)
    {
        try
        {
            var subscriptions = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.Status == SubscriptionStatus.Active &&
                           s.CurrentPeriodEnd <= beforeDate &&
                           !s.CancelAtPeriodEnd)
                .ToListAsync();

            return toSeq(subscriptions);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve expiring subscriptions: {ex.Message}";
        }
    }

    public async Task<Either<string, Subscription>> AddAsync(Subscription subscription)
    {
        try
        {
            await _context.Subscriptions.AddAsync(subscription);
            await _context.SaveChangesAsync();
            return subscription;
        }
        catch (Exception ex)
        {
            return $"Failed to add subscription: {ex.Message}";
        }
    }

    public async Task<Either<string, Subscription>> UpdateAsync(Subscription subscription)
    {
        try
        {
            var existing = await _context.Subscriptions.FindAsync(subscription.Id);

            if (existing == null)
                return "Subscription not found";

            _context.Entry(existing).CurrentValues.SetValues(subscription);
            await _context.SaveChangesAsync();

            return subscription;
        }
        catch (Exception ex)
        {
            return $"Failed to update subscription: {ex.Message}";
        }
    }

    public async Task<Either<string, Unit>> DeleteAsync(Guid id)
    {
        try
        {
            var subscription = await _context.Subscriptions.FindAsync(id);

            if (subscription == null)
                return "Subscription not found";

            _context.Subscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            return unit;
        }
        catch (Exception ex)
        {
            return $"Failed to delete subscription: {ex.Message}";
        }
    }
}
