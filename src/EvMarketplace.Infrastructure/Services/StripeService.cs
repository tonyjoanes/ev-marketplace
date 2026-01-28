using EvMarketplace.Domain.Models;
using LanguageExt;
using Stripe;
using Stripe.Checkout;
using static LanguageExt.Prelude;

namespace EvMarketplace.Infrastructure.Services;

/// <summary>
/// Stripe integration service with functional programming patterns
/// </summary>
public interface IStripeService
{
    // Customer operations
    Task<Either<string, Customer>> CreateCustomerAsync(string email, string name);
    Task<Option<Customer>> GetCustomerAsync(string customerId);

    // Subscription operations
    Task<Either<string, Stripe.Subscription>> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int trialDays = 0);
    Task<Either<string, Stripe.Subscription>> UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId);
    Task<Either<string, Stripe.Subscription>> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately = false);
    Task<Option<Stripe.Subscription>> GetSubscriptionAsync(string subscriptionId);

    // Payment intent operations
    Task<Either<string, PaymentIntent>> CreatePaymentIntentAsync(
        decimal amountGbp,
        string customerId,
        string description);
    Task<Option<PaymentIntent>> GetPaymentIntentAsync(string paymentIntentId);

    // Checkout session operations
    Task<Either<string, Session>> CreateCheckoutSessionAsync(
        string customerId,
        SubscriptionTier tier,
        string successUrl,
        string cancelUrl);
    Task<Either<string, Session>> CreateFeaturedListingCheckoutSessionAsync(
        string customerId,
        FeaturedType featuredType,
        int durationDays,
        string successUrl,
        string cancelUrl);

    // Portal session (for customer to manage subscription)
    Task<Either<string, Stripe.BillingPortal.Session>> CreatePortalSessionAsync(
        string customerId,
        string returnUrl);
}

public class StripeService : IStripeService
{
    private readonly CustomerService _customerService;
    private readonly SubscriptionService _subscriptionService;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly SessionService _sessionService;
    private readonly Stripe.BillingPortal.SessionService _portalSessionService;

    public StripeService(string apiKey)
    {
        StripeConfiguration.ApiKey = apiKey;

        _customerService = new CustomerService();
        _subscriptionService = new SubscriptionService();
        _paymentIntentService = new PaymentIntentService();
        _sessionService = new SessionService();
        _portalSessionService = new Stripe.BillingPortal.SessionService();
    }

    // Customer operations

    public async Task<Either<string, Customer>> CreateCustomerAsync(string email, string name)
    {
        try
        {
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Metadata = new Dictionary<string, string>
                {
                    { "source", "ev-marketplace" },
                    { "created_at", DateTime.UtcNow.ToString("O") }
                }
            };

            var customer = await _customerService.CreateAsync(options);
            return customer;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating customer: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating customer: {ex.Message}";
        }
    }

    public async Task<Option<Customer>> GetCustomerAsync(string customerId)
    {
        try
        {
            var customer = await _customerService.GetAsync(customerId);
            return Optional(customer);
        }
        catch
        {
            return None;
        }
    }

    // Subscription operations

    public async Task<Either<string, Stripe.Subscription>> CreateSubscriptionAsync(
        string customerId,
        string priceId,
        int trialDays = 0)
    {
        try
        {
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new() { Price = priceId }
                },
                PaymentBehavior = "default_incomplete",
                PaymentSettings = new SubscriptionPaymentSettingsOptions
                {
                    SaveDefaultPaymentMethod = "on_subscription"
                },
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            if (trialDays > 0)
            {
                options.TrialPeriodDays = trialDays;
            }

            var subscription = await _subscriptionService.CreateAsync(options);
            return subscription;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating subscription: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating subscription: {ex.Message}";
        }
    }

    public async Task<Either<string, Stripe.Subscription>> UpdateSubscriptionAsync(
        string subscriptionId,
        string newPriceId)
    {
        try
        {
            // Get current subscription
            var subscription = await _subscriptionService.GetAsync(subscriptionId);
            var currentItemId = subscription.Items.Data[0].Id;

            var options = new SubscriptionUpdateOptions
            {
                Items = new List<SubscriptionItemOptions>
                {
                    new()
                    {
                        Id = currentItemId,
                        Price = newPriceId
                    }
                },
                ProrationBehavior = "always_invoice" // Charge/credit immediately
            };

            var updatedSubscription = await _subscriptionService.UpdateAsync(subscriptionId, options);
            return updatedSubscription;
        }
        catch (StripeException ex)
        {
            return $"Stripe error updating subscription: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error updating subscription: {ex.Message}";
        }
    }

    public async Task<Either<string, Stripe.Subscription>> CancelSubscriptionAsync(
        string subscriptionId,
        bool cancelImmediately = false)
    {
        try
        {
            if (cancelImmediately)
            {
                var subscription = await _subscriptionService.CancelAsync(subscriptionId);
                return subscription;
            }
            else
            {
                // Cancel at period end
                var options = new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                };

                var subscription = await _subscriptionService.UpdateAsync(subscriptionId, options);
                return subscription;
            }
        }
        catch (StripeException ex)
        {
            return $"Stripe error canceling subscription: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error canceling subscription: {ex.Message}";
        }
    }

    public async Task<Option<Stripe.Subscription>> GetSubscriptionAsync(string subscriptionId)
    {
        try
        {
            var subscription = await _subscriptionService.GetAsync(subscriptionId);
            return Optional(subscription);
        }
        catch
        {
            return None;
        }
    }

    // Payment intent operations

    public async Task<Either<string, PaymentIntent>> CreatePaymentIntentAsync(
        decimal amountGbp,
        string customerId,
        string description)
    {
        try
        {
            var amountPence = (long)(amountGbp * 100); // Convert to pence

            var options = new PaymentIntentCreateOptions
            {
                Amount = amountPence,
                Currency = "gbp",
                Customer = customerId,
                Description = description,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var paymentIntent = await _paymentIntentService.CreateAsync(options);
            return paymentIntent;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating payment intent: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating payment intent: {ex.Message}";
        }
    }

    public async Task<Option<PaymentIntent>> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId);
            return Optional(paymentIntent);
        }
        catch
        {
            return None;
        }
    }

    // Checkout session operations

    public async Task<Either<string, Session>> CreateCheckoutSessionAsync(
        string customerId,
        SubscriptionTier tier,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            var plan = SubscriptionPlan.Plans.GetPlan(tier);

            if (plan.IsNone)
                return "Invalid subscription tier";

            var priceId = plan.Match(
                Some: p => p.StripePriceId,
                None: () => string.Empty
            );

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                Mode = "subscription",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                BillingAddressCollection = "required",
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 14 // 14-day free trial
                }
            };

            var session = await _sessionService.CreateAsync(options);
            return session;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating checkout session: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating checkout session: {ex.Message}";
        }
    }

    public async Task<Either<string, Session>> CreateFeaturedListingCheckoutSessionAsync(
        string customerId,
        FeaturedType featuredType,
        int durationDays,
        string successUrl,
        string cancelUrl)
    {
        try
        {
            // Calculate price based on featured type
            var weeklyPrice = featuredType switch
            {
                FeaturedType.HomepageHero => 99m,
                FeaturedType.CategoryTop => 49m,
                FeaturedType.SearchBoost => 29m,
                FeaturedType.Spotlight => 15m,
                _ => 0m
            };

            var weeks = Math.Ceiling(durationDays / 7.0m);
            var totalPrice = weeklyPrice * weeks;
            var amountPence = (long)(totalPrice * 100);

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "gbp",
                            UnitAmount = amountPence,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Featured Listing - {featuredType}",
                                Description = $"{durationDays} days of featured placement"
                            }
                        },
                        Quantity = 1
                    }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                BillingAddressCollection = "required"
            };

            var session = await _sessionService.CreateAsync(options);
            return session;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating featured listing checkout: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating featured listing checkout: {ex.Message}";
        }
    }

    // Portal session

    public async Task<Either<string, Stripe.BillingPortal.Session>> CreatePortalSessionAsync(
        string customerId,
        string returnUrl)
    {
        try
        {
            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            };

            var session = await _portalSessionService.CreateAsync(options);
            return session;
        }
        catch (StripeException ex)
        {
            return $"Stripe error creating portal session: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error creating portal session: {ex.Message}";
        }
    }
}
