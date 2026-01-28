# Stripe Webhook Handler Azure Function

This Azure Function handles webhook events from Stripe for subscription and payment processing.

## Events Handled

### Subscription Events
- `customer.subscription.created` - New subscription created
- `customer.subscription.updated` - Subscription updated (tier change, status change)
- `customer.subscription.deleted` - Subscription canceled
- `customer.subscription.trial_will_end` - Trial ending soon (3 days before)

### Payment Events
- `payment_intent.succeeded` - One-time payment successful
- `payment_intent.payment_failed` - Payment failed
- `invoice.paid` - Subscription invoice paid (recurring)
- `invoice.payment_failed` - Subscription payment failed

### Checkout Events
- `checkout.session.completed` - Checkout session completed
- `checkout.session.expired` - Checkout session expired

## Setup

### 1. Local Development

**Install Stripe CLI:**
```bash
# macOS
brew install stripe/stripe-cli/stripe

# Windows (with Chocolatey)
choco install stripe-cli

# Or download from https://github.com/stripe/stripe-cli/releases
```

**Login to Stripe:**
```bash
stripe login
```

**Get webhook secret:**
```bash
# Forward webhooks to local function (port 7071 is default for Functions)
stripe listen --forward-to http://localhost:7071/api/webhook/stripe

# Copy the webhook signing secret (starts with whsec_)
# Update local.settings.json with this secret
```

**Update local.settings.json:**
```json
{
  "Values": {
    "StripeWebhookSecret": "whsec_YOUR_LOCAL_WEBHOOK_SECRET"
  }
}
```

**Run the function:**
```bash
cd infrastructure/functions/StripeWebhook
func start
```

### 2. Testing Webhooks Locally

**Trigger test events:**
```bash
# Test subscription created
stripe trigger customer.subscription.created

# Test payment succeeded
stripe trigger payment_intent.succeeded

# Test invoice paid
stripe trigger invoice.paid

# Test subscription updated
stripe trigger customer.subscription.updated
```

**Or use the Stripe Dashboard:**
1. Go to https://dashboard.stripe.com/test/webhooks
2. Click on your webhook endpoint
3. Click "Send test webhook"
4. Select event type and send

### 3. Production Setup

**Create webhook endpoint in Stripe Dashboard:**
1. Go to https://dashboard.stripe.com/webhooks
2. Click "+ Add endpoint"
3. Enter endpoint URL: `https://func-evmarket-prod.azurewebsites.net/api/webhook/stripe`
4. Select events to listen for (or select "Select all events")
5. Click "Add endpoint"
6. Copy the signing secret

**Add webhook secret to Azure Function configuration:**
```bash
az functionapp config appsettings set \
  --name func-evmarket-prod \
  --resource-group rg-evmarket-prod \
  --settings "StripeWebhookSecret=whsec_YOUR_PROD_SECRET"
```

**Or via Azure Portal:**
1. Navigate to your Function App
2. Go to Configuration → Application settings
3. Add new setting: `StripeWebhookSecret` = `whsec_...`
4. Click Save

## Event Processing Flow

### Subscription Created
1. Receive `customer.subscription.created` event
2. Find Seller by `customer.id` (Stripe customer ID)
3. Create new `Subscription` record
4. Update Seller's `CurrentSubscriptionTier` and `ActiveSubscriptionId`
5. Log event in Application Insights

### Payment Succeeded
1. Receive `payment_intent.succeeded` event
2. Find `Payment` record by `payment_intent.id`
3. Update Payment status to `Succeeded`
4. Set `PaidAt` timestamp
5. If payment is for FeaturedListing, activate it
6. Send confirmation email to seller

### Subscription Updated
1. Receive `customer.subscription.updated` event
2. Find Subscription by Stripe subscription ID
3. Update status, period dates, tier if changed
4. If canceled, revert Seller to free tier
5. If status is `past_due`, send payment reminder

### Invoice Paid (Recurring)
1. Receive `invoice.paid` event
2. Create Payment record for renewal
3. Update Subscription period dates
4. Send invoice receipt to seller

## Monitoring

### Application Insights Queries

**Failed webhook processing:**
```kusto
traces
| where message contains "Failed to process event"
| project timestamp, message, severityLevel
| order by timestamp desc
```

**Payment events:**
```kusto
traces
| where message contains "Payment"
| project timestamp, message
| order by timestamp desc
```

**Webhook volume:**
```kusto
traces
| where message contains "Stripe webhook received"
| summarize count() by bin(timestamp, 1h)
```

### Alerts

Set up alerts for:
- Webhook processing failures (> 5 in 15 min)
- Payment failures (> 10% failure rate)
- Subscription cancelations (spike detection)

## Security

### Webhook Signature Verification

The function verifies all webhook requests using Stripe's signature verification:

```csharp
Event stripeEvent = EventUtility.ConstructEvent(
    json,
    signatureHeader,
    _webhookSecret
);
```

This ensures:
- Requests are from Stripe
- Payloads haven't been tampered with
- Prevents replay attacks

### Function Authorization

Set `AuthorizationLevel.Function` in the trigger:
- Requires function key in URL or `x-functions-key` header
- Stripe automatically includes the key you provide
- Keys can be rotated in Azure Portal

## Troubleshooting

### Webhook not receiving events

**Check Stripe Dashboard:**
- Go to Webhooks → Your endpoint
- Check "Recent deliveries" for failures
- Look at response codes and error messages

**Common issues:**
1. **Invalid signature** - Webhook secret doesn't match
   - Solution: Update `StripeWebhookSecret` setting
2. **Timeout** - Function takes too long to respond
   - Solution: Optimize database queries, use async operations
3. **500 errors** - Internal function error
   - Solution: Check Application Insights logs

### Local testing not working

**Stripe CLI not forwarding:**
```bash
# Check Stripe CLI is running
stripe listen --forward-to http://localhost:7071/api/webhook/stripe

# Verify function is running
curl http://localhost:7071/api/webhook/stripe
```

**Function not starting:**
```bash
# Check .NET SDK installed
dotnet --version

# Restore packages
dotnet restore

# Build function
dotnet build
```

## Database Operations (TODO)

The webhook handlers currently have placeholder TODOs. To complete them:

1. **Add DbContext injection** in constructor
2. **Implement repository pattern** for database updates
3. **Use transactions** for multi-step updates
4. **Handle idempotency** - Stripe may send duplicate events
5. **Queue background jobs** for slow operations (email sending)

Example implementation:

```csharp
private readonly EvMarketplaceDbContext _dbContext;
private readonly ISubscriptionRepository _subscriptionRepo;

private async Task<ProcessResult> HandleSubscriptionCreated(Event stripeEvent)
{
    var subscription = stripeEvent.Data.Object as Stripe.Subscription;

    // Find seller
    var seller = await _dbContext.Sellers
        .FirstOrDefaultAsync(s => s.StripeCustomerId == subscription.CustomerId);

    if (seller == null)
        return ProcessResult.Failure("Seller not found");

    // Create subscription
    var newSub = Subscription.CreateFree(Guid.NewGuid(), seller.Id);
    await _subscriptionRepo.AddAsync(newSub);

    // Update seller
    var updatedSeller = seller.UpdateSubscription(tier, newSub.Id, customerId);
    await _dbContext.SaveChangesAsync();

    return ProcessResult.Success();
}
```

## Cost Optimization

**Consumption Plan (Default):**
- Pay per execution
- First 1 million executions free/month
- $0.20 per million executions after
- Typical cost: £5-10/month for 50k webhooks

**Premium Plan (Production):**
- Always-on instances
- Better cold start performance
- Recommended for high-volume production

## Further Reading

- [Stripe Webhooks Documentation](https://stripe.com/docs/webhooks)
- [Azure Functions HTTP Trigger](https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger)
- [Stripe Event Types](https://stripe.com/docs/api/events/types)
