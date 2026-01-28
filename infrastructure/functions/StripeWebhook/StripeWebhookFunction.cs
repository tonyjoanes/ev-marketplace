using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Net;
using System.Text;

namespace StripeWebhook;

/// <summary>
/// Azure Function to handle Stripe webhook events
/// Processes subscription and payment events with functional error handling
/// </summary>
public class StripeWebhookFunction
{
    private readonly ILogger _logger;
    private readonly string _webhookSecret;

    public StripeWebhookFunction(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<StripeWebhookFunction>();
        _webhookSecret = configuration["StripeWebhookSecret"]
            ?? throw new InvalidOperationException("Stripe webhook secret not configured");
    }

    [Function("StripeWebhook")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "webhook/stripe")]
        HttpRequestData req)
    {
        _logger.LogInformation("Stripe webhook received");

        try
        {
            // Read request body
            var json = await new StreamReader(req.Body).ReadToEndAsync();

            // Get Stripe signature from headers
            var signatureHeader = req.Headers.GetValues("Stripe-Signature").FirstOrDefault();

            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("Missing Stripe signature header");
                return CreateResponse(req, HttpStatusCode.BadRequest, "Missing Stripe signature");
            }

            // Verify webhook signature
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(
                    json,
                    signatureHeader,
                    _webhookSecret,
                    throwOnApiVersionMismatch: false
                );
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Invalid Stripe signature");
                return CreateResponse(req, HttpStatusCode.BadRequest, $"Invalid signature: {ex.Message}");
            }

            // Process event based on type
            _logger.LogInformation($"Processing Stripe event: {stripeEvent.Type}");

            var result = stripeEvent.Type switch
            {
                // Subscription events
                "customer.subscription.created" => await HandleSubscriptionCreated(stripeEvent),
                "customer.subscription.updated" => await HandleSubscriptionUpdated(stripeEvent),
                "customer.subscription.deleted" => await HandleSubscriptionDeleted(stripeEvent),
                "customer.subscription.trial_will_end" => await HandleTrialWillEnd(stripeEvent),

                // Payment events
                "payment_intent.succeeded" => await HandlePaymentSucceeded(stripeEvent),
                "payment_intent.payment_failed" => await HandlePaymentFailed(stripeEvent),

                // Checkout events
                "checkout.session.completed" => await HandleCheckoutCompleted(stripeEvent),
                "checkout.session.expired" => await HandleCheckoutExpired(stripeEvent),

                // Invoice events (for subscriptions)
                "invoice.paid" => await HandleInvoicePaid(stripeEvent),
                "invoice.payment_failed" => await HandleInvoicePaymentFailed(stripeEvent),

                // Catch-all for unhandled events
                _ => HandleUnhandledEvent(stripeEvent)
            };

            if (result.IsSuccess)
            {
                _logger.LogInformation($"Successfully processed event: {stripeEvent.Type}");
                return CreateResponse(req, HttpStatusCode.OK, "Event processed successfully");
            }
            else
            {
                _logger.LogError($"Failed to process event: {result.ErrorMessage}");
                return CreateResponse(req, HttpStatusCode.InternalServerError, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return CreateResponse(req, HttpStatusCode.InternalServerError, "Internal server error");
        }
    }

    // Subscription event handlers

    private async Task<ProcessResult> HandleSubscriptionCreated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
            return ProcessResult.Failure("Invalid subscription data");

        _logger.LogInformation($"Subscription created: {subscription.Id}");

        // TODO: Update database with new subscription
        // - Find seller by Stripe customer ID
        // - Create Subscription record
        // - Update Seller with subscription tier

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
            return ProcessResult.Failure("Invalid subscription data");

        _logger.LogInformation($"Subscription updated: {subscription.Id}, Status: {subscription.Status}");

        // TODO: Update database with subscription changes
        // - Update subscription status
        // - Update current period dates
        // - Handle tier changes
        // - Update Seller if subscription canceled

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
            return ProcessResult.Failure("Invalid subscription data");

        _logger.LogInformation($"Subscription deleted: {subscription.Id}");

        // TODO: Handle subscription cancellation
        // - Update subscription status to Canceled
        // - Revert seller to free tier
        // - Disable any active listings beyond free tier limit

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandleTrialWillEnd(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (subscription == null)
            return ProcessResult.Failure("Invalid subscription data");

        _logger.LogInformation($"Trial ending soon for subscription: {subscription.Id}");

        // TODO: Send notification to seller
        // - Get seller email
        // - Queue email notification about trial ending
        // - Remind them to add payment method

        return ProcessResult.Success();
    }

    // Payment event handlers

    private async Task<ProcessResult> HandlePaymentSucceeded(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
            return ProcessResult.Failure("Invalid payment intent data");

        _logger.LogInformation($"Payment succeeded: {paymentIntent.Id}, Amount: {paymentIntent.Amount}");

        // TODO: Record successful payment
        // - Find Payment record by Stripe payment intent ID
        // - Update status to Succeeded
        // - Set PaidAt timestamp
        // - If for featured listing, activate it

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandlePaymentFailed(Event stripeEvent)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
            return ProcessResult.Failure("Invalid payment intent data");

        _logger.LogWarning($"Payment failed: {paymentIntent.Id}");

        // TODO: Handle payment failure
        // - Update Payment status to Failed
        // - Notify seller of failure
        // - If subscription payment, mark subscription as past_due

        return ProcessResult.Success();
    }

    // Checkout event handlers

    private async Task<ProcessResult> HandleCheckoutCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null)
            return ProcessResult.Failure("Invalid checkout session data");

        _logger.LogInformation($"Checkout completed: {session.Id}, Customer: {session.CustomerId}");

        // TODO: Handle successful checkout
        // - If subscription checkout, subscription.created event will handle it
        // - If one-time payment, create Payment record
        // - Activate any pending featured listings

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandleCheckoutExpired(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
        if (session == null)
            return ProcessResult.Failure("Invalid checkout session data");

        _logger.LogInformation($"Checkout expired: {session.Id}");

        // TODO: Handle expired checkout
        // - Clean up any pending records
        // - Optional: Send reminder email

        return ProcessResult.Success();
    }

    // Invoice event handlers

    private async Task<ProcessResult> HandleInvoicePaid(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
            return ProcessResult.Failure("Invalid invoice data");

        _logger.LogInformation($"Invoice paid: {invoice.Id}, Amount: {invoice.AmountPaid}");

        // TODO: Record invoice payment
        // - Create Payment record for subscription renewal
        // - Update subscription current period

        return ProcessResult.Success();
    }

    private async Task<ProcessResult> HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null)
            return ProcessResult.Failure("Invalid invoice data");

        _logger.LogWarning($"Invoice payment failed: {invoice.Id}");

        // TODO: Handle failed invoice payment
        // - Update subscription status
        // - Notify seller
        // - Begin grace period countdown

        return ProcessResult.Success();
    }

    // Unhandled events

    private ProcessResult HandleUnhandledEvent(Event stripeEvent)
    {
        _logger.LogInformation($"Unhandled event type: {stripeEvent.Type}");
        return ProcessResult.Success(); // Don't fail on unhandled events
    }

    // Helper methods

    private HttpResponseData CreateResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString($"{{\"message\": \"{message}\"}}");
        return response;
    }
}

// Result type for processing events
public record ProcessResult(bool IsSuccess, string ErrorMessage)
{
    public static ProcessResult Success() => new(true, string.Empty);
    public static ProcessResult Failure(string error) => new(false, error);
}
