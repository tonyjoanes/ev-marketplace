# Database Documentation

## Overview

The EV Marketplace uses **PostgreSQL** as its database, managed through **Entity Framework Core** with a functional programming approach.

## Database Schema

### ElectricVehicles Table

| Column | Type | Description |
|--------|------|-------------|
| Id | UUID | Primary key |
| Make | VARCHAR(100) | Vehicle manufacturer (indexed) |
| Model | VARCHAR(100) | Vehicle model name |
| Year | INTEGER | Model year |
| BatteryCapacityKwh | DECIMAL(10,2) | Battery capacity in kilowatt-hours |
| WltpRangeKm | DECIMAL(10,2) | WLTP range in kilometers |
| EfficiencyKwhPer100Km | DECIMAL(10,2) | Energy efficiency |
| AcChargeRateKw | DECIMAL(10,2) | AC charging rate in kilowatts |
| DcChargeRateKw | DECIMAL(10,2) | DC fast charging rate in kilowatts |
| ConnectorTypes | VARCHAR(200) | Comma-separated connector types |
| BodyType | VARCHAR(50) | Vehicle body type (indexed) |
| PriceGbp | DECIMAL(10,2) | Price in GBP (indexed) |

### Indexes

- `IX_ElectricVehicles_Make` - For filtering by manufacturer
- `IX_ElectricVehicles_BodyType` - For filtering by body type
- `IX_ElectricVehicles_PriceGbp` - For price range queries

## Development Setup

### Using .NET Aspire (Recommended)

The easiest way to run the database locally is using .NET Aspire:

```bash
# Start Aspire AppHost (includes PostgreSQL + API)
dotnet run --project src/EvMarketplace.AppHost
```

This will:
- Start a PostgreSQL container with pgAdmin
- Apply migrations automatically
- Seed the database with sample data
- Start the API on http://localhost:5000

Access pgAdmin at: http://localhost:5050 (check Aspire dashboard for credentials)

### Manual PostgreSQL Setup

If you prefer to run PostgreSQL manually:

```bash
# Start PostgreSQL with Docker
docker run --name evmarketplace-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=evmarketplace \
  -p 5432:5432 \
  -d postgres:16

# Apply migrations
dotnet ef database update --project src/EvMarketplace.Infrastructure --startup-project src/EvMarketplace.Api
```

## Working with Migrations

### Creating a New Migration

When you modify the domain models, create a new migration:

```bash
dotnet ef migrations add MigrationName \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api \
  --output-dir Migrations
```

### Applying Migrations

Migrations are automatically applied on startup in development mode. For manual application:

```bash
dotnet ef database update \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api
```

### Rolling Back Migrations

```bash
# Roll back to a specific migration
dotnet ef database update MigrationName \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api

# Roll back all migrations
dotnet ef database update 0 \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api
```

### Removing the Last Migration

```bash
dotnet ef migrations remove \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api
```

## Seed Data

The database is automatically seeded with 15 sample EVs on first run in development mode:

- Tesla Model 3 Long Range, Model Y Performance
- Volkswagen ID.3, ID.4
- BMW i4, iX
- Hyundai Ioniq 5, Kona Electric
- Kia EV6
- Nissan Ariya
- MG MG4
- Polestar 2
- Mercedes-Benz EQE
- Audi Q4 e-tron
- Ford Mustang Mach-E

Seed data logic is in `src/EvMarketplace.Infrastructure/Data/SeedData.cs`

## Connection Strings

### Development (Aspire)
Connection string is automatically injected by Aspire. No manual configuration needed.

### Development (Manual)
Update `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=evmarketplace;Username=postgres;Password=postgres"
  }
}
```

### Production
Connection strings should be configured via:
- Azure App Configuration
- Key Vault references
- Environment variables

Never commit production connection strings to source control.

## Functional Repository Pattern

The repository uses LanguageExt types for functional error handling:

```csharp
// Returns Either<string, Seq<ElectricVehicle>>
var result = await repository.GetAllAsync();

result.Match(
    Right: vehicles => /* success */,
    Left: error => /* handle error */
);

// Returns Option<ElectricVehicle>
var vehicle = await repository.GetByIdAsync(id);

vehicle.Match(
    Some: ev => /* found */,
    None: () => /* not found */
);
```

## Database Utilities

### Resetting the Database

```bash
# Drop and recreate database
dotnet ef database drop --force \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api

dotnet ef database update \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api
```

### Viewing Database Schema

```bash
# Generate SQL script for current migration
dotnet ef migrations script \
  --project src/EvMarketplace.Infrastructure \
  --startup-project src/EvMarketplace.Api \
  --output schema.sql
```

## Troubleshooting

### Port Already in Use
If port 5432 is already in use, either:
1. Stop the existing PostgreSQL instance
2. Use Aspire which manages ports automatically

### Migration Errors
If migrations fail to apply:
1. Ensure PostgreSQL is running
2. Check connection string
3. Verify your user has necessary permissions
4. Check for conflicting migrations

### Seed Data Not Appearing
Seed data only runs in Development environment. Check:
1. `ASPNETCORE_ENVIRONMENT=Development`
2. Database is empty (seed only runs on empty database)
3. Check application logs for seed errors
