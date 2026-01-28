using EvMarketplace.Domain.Models;
using EvMarketplace.Infrastructure.Data;
using LanguageExt;
using Microsoft.EntityFrameworkCore;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Repositories;

public interface IPaymentRepository
{
    Task<Option<Payment>> GetByIdAsync(Guid id);
    Task<Option<Payment>> GetByStripePaymentIntentIdAsync(string stripePaymentIntentId);
    Task<Either<string, Seq<Payment>>> GetBySellerIdAsync(Guid sellerId);
    Task<Either<string, Seq<Payment>>> GetRecentPaymentsAsync(int count = 100);
    Task<Either<string, PaymentStats>> GetPaymentStatsAsync(DateTime from, DateTime to);
    Task<Either<string, Payment>> AddAsync(Payment payment);
    Task<Either<string, Payment>> UpdateAsync(Payment payment);
}

public record PaymentStats(
    decimal TotalRevenue,
    int TotalPayments,
    int SuccessfulPayments,
    int FailedPayments,
    decimal AveragePaymentAmount,
    Dictionary<PaymentType, decimal> RevenueByType
);

public class PaymentRepository : IPaymentRepository
{
    private readonly EvMarketplaceDbContext _context;

    public PaymentRepository(EvMarketplaceDbContext context)
    {
        _context = context;
    }

    public async Task<Option<Payment>> GetByIdAsync(Guid id)
    {
        try
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return Optional(payment);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Option<Payment>> GetByStripePaymentIntentIdAsync(string stripePaymentIntentId)
    {
        try
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == stripePaymentIntentId);

            return Optional(payment);
        }
        catch
        {
            return None;
        }
    }

    public async Task<Either<string, Seq<Payment>>> GetBySellerIdAsync(Guid sellerId)
    {
        try
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return toSeq(payments);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve payments for seller: {ex.Message}";
        }
    }

    public async Task<Either<string, Seq<Payment>>> GetRecentPaymentsAsync(int count = 100)
    {
        try
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();

            return toSeq(payments);
        }
        catch (Exception ex)
        {
            return $"Failed to retrieve recent payments: {ex.Message}";
        }
    }

    public async Task<Either<string, PaymentStats>> GetPaymentStatsAsync(DateTime from, DateTime to)
    {
        try
        {
            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
                .ToListAsync();

            var successfulPayments = payments.Where(p => p.Status == PaymentStatus.Succeeded).ToList();

            var revenueByType = successfulPayments
                .GroupBy(p => p.Type)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.AmountGbp)
                );

            var stats = new PaymentStats(
                TotalRevenue: successfulPayments.Sum(p => p.AmountGbp),
                TotalPayments: payments.Count,
                SuccessfulPayments: successfulPayments.Count,
                FailedPayments: payments.Count(p => p.Status == PaymentStatus.Failed),
                AveragePaymentAmount: successfulPayments.Any()
                    ? successfulPayments.Average(p => p.AmountGbp)
                    : 0m,
                RevenueByType: revenueByType
            );

            return stats;
        }
        catch (Exception ex)
        {
            return $"Failed to calculate payment stats: {ex.Message}";
        }
    }

    public async Task<Either<string, Payment>> AddAsync(Payment payment)
    {
        try
        {
            await _context.Payments.AddAsync(payment);
            await _context.SaveChangesAsync();
            return payment;
        }
        catch (Exception ex)
        {
            return $"Failed to add payment: {ex.Message}";
        }
    }

    public async Task<Either<string, Payment>> UpdateAsync(Payment payment)
    {
        try
        {
            var existing = await _context.Payments.FindAsync(payment.Id);

            if (existing == null)
                return "Payment not found";

            _context.Entry(existing).CurrentValues.SetValues(payment);
            await _context.SaveChangesAsync();

            return payment;
        }
        catch (Exception ex)
        {
            return $"Failed to update payment: {ex.Message}";
        }
    }
}
