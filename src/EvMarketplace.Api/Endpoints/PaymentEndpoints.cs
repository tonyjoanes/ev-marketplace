using EvMarketplace.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EvMarketplace.Api.Endpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        group.MapGet("/seller/{sellerId:guid}", GetSellerPayments)
            .WithName("GetSellerPayments")
            .WithOpenApi();

        group.MapGet("/{id:guid}", GetPayment)
            .WithName("GetPayment")
            .WithOpenApi();

        group.MapGet("/recent", GetRecentPayments)
            .WithName("GetRecentPayments")
            .WithOpenApi();

        group.MapGet("/stats", GetPaymentStats)
            .WithName("GetPaymentStats")
            .WithOpenApi();
    }

    private static async Task<IResult> GetSellerPayments(
        Guid sellerId,
        IPaymentRepository paymentRepository)
    {
        var result = await paymentRepository.GetBySellerIdAsync(sellerId);

        return result.Match(
            Right: payments => Results.Ok(payments),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetPayment(
        Guid id,
        IPaymentRepository paymentRepository)
    {
        var paymentOption = await paymentRepository.GetByIdAsync(id);

        return paymentOption.Match(
            Some: payment => Results.Ok(payment),
            None: () => Results.NotFound(new { error = "Payment not found" })
        );
    }

    private static async Task<IResult> GetRecentPayments(
        [FromQuery] int? count,
        IPaymentRepository paymentRepository)
    {
        var result = await paymentRepository.GetRecentPaymentsAsync(count ?? 100);

        return result.Match(
            Right: payments => Results.Ok(payments),
            Left: error => Results.Problem(error)
        );
    }

    private static async Task<IResult> GetPaymentStats(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        IPaymentRepository paymentRepository)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var result = await paymentRepository.GetPaymentStatsAsync(fromDate, toDate);

        return result.Match(
            Right: stats => Results.Ok(new
            {
                periodStart = fromDate,
                periodEnd = toDate,
                totalRevenue = stats.TotalRevenue,
                totalPayments = stats.TotalPayments,
                successfulPayments = stats.SuccessfulPayments,
                failedPayments = stats.FailedPayments,
                averagePaymentAmount = stats.AveragePaymentAmount,
                successRate = stats.TotalPayments > 0
                    ? (decimal)stats.SuccessfulPayments / stats.TotalPayments * 100
                    : 0,
                revenueByType = stats.RevenueByType
            }),
            Left: error => Results.Problem(error)
        );
    }
}
