# Future Integrations Roadmap

This document outlines potential third-party integrations to enhance the EV Marketplace platform. Each integration is categorized by priority and includes implementation considerations.

## High Priority Integrations

### 1. Charger Location & Availability

**Purpose**: Help users find compatible charging stations near their location or along routes.

**Recommended Providers**:
- **Zap-Map API** (UK-focused, most comprehensive)
  - Coverage: 50,000+ charge points across UK
  - Features: Real-time availability, pricing, network info, photos, reviews
  - API: Commercial API available on request
- **Open Charge Map API** (https://openchargemap.org/site/develop/api)
  - Coverage: Global, open-source
  - Features: Free, community-driven, good coverage
  - API: Public REST API, no key required
- **PlugShare API**
  - Coverage: Global
  - Features: User reviews, photos, trip planning

**Implementation Ideas**:
```csharp
// Domain models
public record ChargingStation(
    Guid Id,
    string Name,
    decimal Latitude,
    decimal Longitude,
    Seq<string> ConnectorTypes,
    Option<decimal> PowerKw,
    string NetworkOperator,
    Option<decimal> CostPerKwh,
    bool IsAvailable
);

// Service interface
public interface IChargingLocationService
{
    Task<Either<IntegrationError, Seq<ChargingStation>>> GetNearbyStationsAsync(
        decimal latitude,
        decimal longitude,
        decimal radiusKm,
        Option<Seq<string>> requiredConnectors = default);

    Task<Either<IntegrationError, Seq<ChargingStation>>> GetStationsAlongRouteAsync(
        RouteRequest route,
        decimal maxDetourKm = 5);
}
```

**Endpoints**:
- `GET /api/vehicles/{id}/nearby-chargers?lat={lat}&lon={lon}&radius={km}` - Compatible chargers for a specific EV
- `GET /api/chargers/search?lat={lat}&lon={lon}&connectorType={type}` - General charger search
- `POST /api/chargers/route` - Find chargers along a route

---

### 2. Real-Time Pricing & Inventory

**Purpose**: Show live market prices and available stock from dealers.

**Recommended Providers**:
- **Auto Trader API** (UK market leader)
  - Coverage: 400,000+ vehicles
  - Features: Stock availability, dealer info, valuations
  - API: Commercial partnership required
- **CarGurus API**
  - Features: Price analysis, dealer ratings, market insights
- **Motors.co.uk API**
  - Coverage: UK dealer network

**Implementation Ideas**:
```csharp
public record VehicleListing(
    string Make,
    string Model,
    int Year,
    decimal Mileage,
    decimal PriceGbp,
    string DealerName,
    string Location,
    Option<string> ListingUrl,
    DateTime LastUpdated
);

public interface IPricingService
{
    Task<Either<IntegrationError, Seq<VehicleListing>>> GetCurrentListingsAsync(
        string make,
        string model,
        int year,
        Option<string> postcode = default);

    Task<Either<IntegrationError, PriceAnalysis>> GetPriceAnalysisAsync(
        string make,
        string model,
        int year);
}

public record PriceAnalysis(
    decimal AveragePriceGbp,
    decimal MinPriceGbp,
    decimal MaxPriceGbp,
    int TotalListings,
    decimal MedianPriceGbp
);
```

**Endpoints**:
- `GET /api/vehicles/{id}/market-listings?postcode={postcode}` - Find similar vehicles for sale
- `GET /api/vehicles/{id}/price-analysis` - Price trends and analysis
- `GET /api/pricing/valuation` - Estimate vehicle value

---

### 3. Energy Tariff Data

**Purpose**: Provide accurate running cost calculations using real energy tariffs.

**Recommended Providers**:
- **Octopus Energy API** (https://developer.octopus.energy/docs/api/)
  - Features: Public API, real-time tariffs, EV-specific plans
  - API: Free, well-documented
  - Tariffs: Intelligent Octopus Go, Agile Octopus, etc.
- **Energy Price Guarantee data**
  - Source: Ofgem price cap data
- **Comparison site APIs**
  - uSwitch, MoneySuperMarket

**Implementation Ideas**:
```csharp
public record EnergyTariff(
    string Provider,
    string TariffName,
    decimal StandardRatePencePerKwh,
    Option<decimal> OffPeakRatePencePerKwh,
    Option<TimeSpan> OffPeakStartTime,
    Option<TimeSpan> OffPeakEndTime,
    bool IsEvSpecific,
    decimal StandingChargePencePerDay
);

public interface IEnergyTariffService
{
    Task<Either<IntegrationError, Seq<EnergyTariff>>> GetCurrentTariffsAsync(
        Option<string> postcode = default);

    Task<Either<IntegrationError, EnergyTariff>> GetRecommendedTariffAsync(
        decimal annualMileageKm,
        bool hasSmartCharger);
}
```

**Enhanced Calculator Endpoints**:
- `POST /api/calculators/running-cost-with-tariff` - Use real tariff data
- `GET /api/tariffs/current` - List available EV tariffs
- `GET /api/tariffs/recommend?annualMileage={km}` - Recommend best tariff

---

## Medium Priority Integrations

### 4. Government Incentives & Grants

**Purpose**: Show available grants, tax benefits, and incentives.

**Data Sources**:
- **GOV.UK APIs**
  - Plug-in grant eligibility (currently not available for cars, but may return)
  - Road tax (VED) rates
  - Company car tax (BIK) rates
- **Regional grant schemes** (Scotland, Wales, local councils)

**Implementation Ideas**:
```csharp
public record Incentive(
    string Name,
    string Description,
    decimal AmountGbp,
    IncentiveType Type, // Grant, TaxBenefit, Exemption
    Seq<string> EligibilityCriteria,
    Option<DateTime> ExpiryDate,
    string Region
);

public enum IncentiveType
{
    PurchaseGrant,
    TaxExemption,
    CompanyCarTax,
    CongestionChargeExemption,
    ParkingDiscount
}
```

**Endpoints**:
- `GET /api/vehicles/{id}/incentives` - Applicable incentives for a vehicle
- `GET /api/incentives/calculator?price={price}&co2={co2}` - Calculate total incentives

---

### 5. Vehicle Data Services

**Purpose**: Enrich vehicle data with detailed specifications and verification.

**Providers**:
- **DVLA Vehicle Enquiry Service** (https://www.gov.uk/get-vehicle-information-from-dvla)
  - Features: Registration lookup, MOT history, tax status
  - API: Public API available
- **VIN Decoder APIs**
  - NHTSA, Carquery, VINQuery
  - Features: Full specification sheets

**Endpoints**:
- `GET /api/vehicles/lookup/registration/{vrm}` - Look up by UK registration
- `GET /api/vehicles/lookup/vin/{vin}` - VIN decoder

---

### 6. Insurance Comparison

**Purpose**: Provide instant insurance quotes for EVs.

**Providers**:
- Compare The Market API
- MoneySuperMarket API
- Confused.com API

**Implementation Ideas**:
```csharp
public record InsuranceQuote(
    string Provider,
    decimal AnnualPremiumGbp,
    string CoverType, // Comprehensive, ThirdParty, etc.
    Option<decimal> VoluntaryExcessGbp,
    Seq<string> Features
);
```

---

### 7. Finance & Leasing

**Purpose**: Help users understand financing options.

**Providers**:
- Finance calculators with real APR rates
- LeaseLoco API
- Leasing.com

**Implementation Ideas**:
```csharp
public record FinanceOption(
    FinanceType Type, // PCP, HP, Lease
    decimal MonthlyPaymentGbp,
    int TermMonths,
    decimal DepositGbp,
    decimal AprPercent,
    Option<decimal> FinalPaymentGbp
);

public enum FinanceType
{
    PersonalContractPurchase, // PCP
    HirePurchase, // HP
    PersonalLease,
    BusinessLease
}
```

---

### 8. Weather & Range Adjustment

**Purpose**: Provide realistic range estimates based on weather conditions.

**Providers**:
- **Met Office DataPoint API** (UK weather data)
- **OpenWeather API**

**Implementation Ideas**:
```csharp
public static class WeatherAdjustedRange
{
    public static Either<string, decimal> CalculateAdjustedRange(
        decimal wltpRangeKm,
        decimal temperatureCelsius,
        bool heatingOrCoolingNeeded)
    {
        // Temperature impact on battery efficiency
        // Below 0°C: -20 to -40% range
        // 0-10°C: -10 to -20% range
        // 10-25°C: Optimal (minimal impact)
        // Above 25°C: -5 to -15% range (cooling needed)

        var tempFactor = temperatureCelsius switch
        {
            < 0 => 0.7m,
            < 10 => 0.85m,
            < 25 => 1.0m,
            _ => 0.9m
        };

        var climateFactor = heatingOrCoolingNeeded ? 0.9m : 1.0m;

        return wltpRangeKm * tempFactor * climateFactor;
    }
}
```

---

## Nice-to-Have Integrations

### 9. Reviews & Ratings

**Providers**:
- Carwow API
- Top Gear
- What Car?
- User review aggregation

---

### 10. Dealer & Test Drive Booking

**Features**:
- Direct dealer integration
- Calendar booking for test drives
- Virtual showroom tours

---

### 11. Route Planning

**Providers**:
- Google Maps API / Here Maps
- Features: Range-aware routing, charging stop suggestions

**Implementation Ideas**:
```csharp
public record RouteRequest(
    Location Start,
    Location End,
    Guid VehicleId
);

public record RouteResponse(
    decimal TotalDistanceKm,
    TimeSpan EstimatedDuration,
    Seq<ChargingStop> RequiredChargingStops,
    decimal EstimatedEnergyCost
);

public record ChargingStop(
    ChargingStation Station,
    decimal ChargeNeededKwh,
    TimeSpan ChargingTime,
    int StopNumber
);
```

---

### 12. Charging Network Memberships

**Providers**:
- Ionity
- Gridserve Electric Highway
- BP Pulse
- Shell Recharge

**Features**:
- Membership pricing comparison
- RFID card management
- Subscription benefits

---

### 13. Carbon Footprint & Sustainability

**Data Sources**:
- National Grid ESO Carbon Intensity API
- Lifecycle emissions databases

**Implementation Ideas**:
```csharp
public record CarbonSavings(
    decimal AnnualCo2SavedKg,
    decimal LifetimeCo2SavedKg,
    decimal EquivalentTreesPlanted,
    decimal CurrentGridCarbonIntensityGCo2PerKwh
);
```

---

### 14. Notifications & Alerts

**Providers**:
- Twilio (SMS)
- SendGrid (Email)
- Firebase Cloud Messaging (Push)

**Use Cases**:
- Price drop alerts
- New stock notifications
- Charging reminders
- Service reminders

---

## Technical Architecture for Integrations

### Functional Integration Pattern

All integrations should follow functional patterns with proper error handling:

```csharp
namespace EvMarketplace.Domain.Integrations;

// Common error types
public abstract record IntegrationError
{
    public record NetworkError(string Message) : IntegrationError;
    public record AuthenticationError(string Message) : IntegrationError;
    public record RateLimitExceeded(DateTime RetryAfter) : IntegrationError;
    public record InvalidResponse(string Message) : IntegrationError;
    public record ServiceUnavailable(string ServiceName) : IntegrationError;
}

// Base interface for all integrations
public interface IExternalService
{
    string ServiceName { get; }
    Task<Either<IntegrationError, Unit>> HealthCheckAsync();
}

// Resilience extensions
public static class IntegrationExtensions
{
    /// <summary>
    /// Retry operation with exponential backoff
    /// </summary>
    public static async Task<Either<IntegrationError, T>> WithRetry<T>(
        this Task<Either<IntegrationError, T>> operation,
        int maxRetries = 3,
        int baseDelayMs = 1000)
    {
        var attempts = 0;
        while (attempts < maxRetries)
        {
            var result = await operation;

            if (result.IsRight)
                return result;

            attempts++;
            if (attempts < maxRetries)
            {
                var delay = baseDelayMs * (int)Math.Pow(2, attempts - 1);
                await Task.Delay(delay);
            }
        }

        return await operation;
    }

    /// <summary>
    /// Fallback to cached data if operation fails
    /// </summary>
    public static async Task<Either<IntegrationError, T>> WithCacheFallback<T>(
        this Task<Either<IntegrationError, T>> operation,
        IDistributedCache cache,
        string cacheKey,
        TimeSpan? maxCacheAge = null)
    {
        var result = await operation;

        return await result.Match(
            Right: async data =>
            {
                // Cache successful result
                await cache.SetAsync(cacheKey, data);
                return Right<IntegrationError, T>(data);
            },
            Left: async error =>
            {
                // Try to get from cache
                var cached = await cache.GetAsync<T>(cacheKey);
                return cached.Match(
                    Some: cachedData => Right<IntegrationError, T>(cachedData),
                    None: () => Left<IntegrationError, T>(error)
                );
            }
        );
    }

    /// <summary>
    /// Circuit breaker pattern - stop calling failing service
    /// </summary>
    public static Task<Either<IntegrationError, T>> WithCircuitBreaker<T>(
        this Task<Either<IntegrationError, T>> operation,
        ICircuitBreakerPolicy policy)
    {
        // Implementation would use Polly or similar
        return operation;
    }

    /// <summary>
    /// Timeout after specified duration
    /// </summary>
    public static async Task<Either<IntegrationError, T>> WithTimeout<T>(
        this Task<Either<IntegrationError, T>> operation,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await operation;
        }
        catch (OperationCanceledException)
        {
            return new IntegrationError.NetworkError("Operation timed out");
        }
    }
}
```

### Configuration Pattern

Store API keys and configuration securely:

```csharp
// appsettings.json structure
{
  "Integrations": {
    "ZapMap": {
      "ApiKey": "{{ZAPMAP_API_KEY}}",
      "BaseUrl": "https://api.zap-map.com/v1",
      "TimeoutSeconds": 30,
      "EnableCaching": true,
      "CacheDurationMinutes": 15
    },
    "OctopusEnergy": {
      "ApiKey": "{{OCTOPUS_API_KEY}}",
      "BaseUrl": "https://api.octopus.energy/v1",
      "TimeoutSeconds": 10,
      "EnableCaching": true,
      "CacheDurationMinutes": 60
    }
  }
}

// Configuration classes
public record IntegrationConfig(
    string ApiKey,
    string BaseUrl,
    int TimeoutSeconds,
    bool EnableCaching,
    int CacheDurationMinutes
);
```

### Caching Strategy

```csharp
public interface IIntegrationCache
{
    Task<Option<T>> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}

// Cache keys should be namespaced
public static class CacheKeys
{
    public static string ChargingStations(decimal lat, decimal lon, decimal radius) =>
        $"charging:stations:{lat:F4}:{lon:F4}:{radius}";

    public static string EnergyTariffs(string provider) =>
        $"energy:tariffs:{provider}";

    public static string VehicleListings(string make, string model, int year) =>
        $"pricing:listings:{make}:{model}:{year}";
}
```

### Monitoring & Logging

```csharp
public interface IIntegrationMonitor
{
    void RecordSuccess(string serviceName, TimeSpan duration);
    void RecordFailure(string serviceName, IntegrationError error);
    Task<IntegrationHealth> GetServiceHealthAsync(string serviceName);
}

public record IntegrationHealth(
    string ServiceName,
    bool IsHealthy,
    decimal SuccessRate,
    TimeSpan AverageResponseTime,
    DateTime LastSuccessfulCall,
    Option<DateTime> LastFailure
);
```

## Implementation Priority

Recommended implementation order:

1. **Phase 1** (MVP Enhancement):
   - Open Charge Map API (free, immediate value)
   - Octopus Energy API (free, accurate cost calculations)
   - Weather-adjusted range (Met Office API)

2. **Phase 2** (Market Research):
   - Auto Trader API or similar (requires commercial agreement)
   - DVLA Vehicle Enquiry Service
   - GOV.UK incentive data

3. **Phase 3** (User Engagement):
   - Route planning with charging stops
   - Notifications & alerts
   - Review aggregation

4. **Phase 4** (Monetization):
   - Insurance comparison
   - Finance & leasing
   - Dealer partnerships

## Security Considerations

- Store API keys in Azure Key Vault or similar
- Use managed identities where possible
- Implement rate limiting to protect against abuse
- Log all integration calls for audit
- Monitor for suspicious patterns
- Implement proper error messages that don't leak sensitive info

## Cost Considerations

- Track API usage and costs per integration
- Implement caching aggressively for expensive APIs
- Use free/public APIs where possible
- Consider fallback to scraped data if APIs are too expensive
- Monitor and alert on unusual usage patterns

---

**Document Version**: 1.0
**Last Updated**: January 2026
**Next Review**: Quarterly or when adding new integration
