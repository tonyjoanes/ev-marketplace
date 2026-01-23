# EV Marketplace

## Vision

Build a browseable EV catalogue where users can search, filter, compare cars, and run calculators (running cost, charging time, simple TCO), while you practice modern React/Nx, functional C#, Azure DevOps, and cloud deployment.

## Tech Stack

- **Frontend**: React in an Nx workspace (TypeScript, component libraries, shared UI).
- **Backend**: .NET 8, C#, "functional core, imperative shell" style using language-ext (or similar) for options, results, immutability, and pure functions.
- **Orchestration (local)**: .NET Aspire to run API + DB (and optionally frontend) together.
- **Data**: PostgreSQL or SQL Server via EF Core.
- **DevOps**:
  - Code: GitHub (primary repo).
  - CI/CD: Azure DevOps pipelines (GitHub as external source).
  - Packages: Azure Artifacts for internal NuGet (shared calculation/domain library).
- **Cloud (later)**: Azure App Service or Azure Container Apps + managed DB.

## Functional C# Approach

- Prefer pure functions over mutable OO services for domain logic ("functional core, imperative shell").
- Use language-ext-style primitives (e.g. `Option`, `Either/Result`, immutable collections) to model success/failure and avoid null/exception-driven flows.
- Keep side effects (I/O, DB, HTTP) at the boundaries and compose them via small, testable functions.

## Core Features (MVP)

- **EV catalogue**:
  - EV models with specs: make, model, year, battery (kWh), WLTP range, efficiency, AC/DC charge rates, connector types, body type, price.
  - API endpoints for listing, filtering, and viewing details.
- **Search & filters**:
  - Filter by make, price range, range, body type.
- **Calculators**:
  - Monthly running cost (annual mileage + p/kWh).
  - Charging time 10–80% based on battery size and charger kW.
  - Simple TCO over N years (purchase + energy only).
- **Comparison**:
  - Compare two cars side by side, including calculator outputs.
- **Admin/seed**:
  - Seed a handful of EVs via script or migration.

## Repository Structure

Mono-repo with both front and back ends:

- `apps/web` – React app (Nx-managed).
- `libs/ui` – Shared React UI components.
- `libs/shared` – Shared frontend utils/types.
- `src/EvMarketplace.Api` – ASP.NET Core API (functional-style endpoints + domain orchestration).
- `src/EvMarketplace.Domain` – Pure functions, types, calculators (functional core).
- `src/EvMarketplace.Infrastructure` – EF Core, DB access, configuration (imperative shell).
- `src/EvMarketplace.AppHost` – .NET Aspire AppHost.
- `tests/EvMarketplace.Tests` – Unit tests for domain + calculators.

Nx runs the frontend side; .NET solution handles backend projects.

## Dev Workflow

- **Local**:
  - Run .NET Aspire to start API + DB.
  - Run `nx serve web` for the React frontend.
- **CI/CD (Azure DevOps)**:
  - Trigger on GitHub pushes/PRs.
  - Jobs:
    - Install Node deps and run `nx lint/test/build` for frontend.
    - Build .NET solution, run tests, publish API artifacts.
  - Later: multi-stage YAML to deploy API + web to Azure, with infra-as-code and environment spin-up/tear-down.

## Roadmap

1. Scaffold Nx React workspace and basic .NET solution.
2. Implement functional domain model + calculators with tests.
3. Build minimal API endpoints and DB integration.
4. Wire React pages (browse, detail, compare) to API.
5. Add Aspire host for local multi-service run.
6. Set up Azure DevOps CI, then add deployment and Azure Artifacts publishing.

## Getting Started

### Prerequisites

- Node.js 18+ and npm/yarn
- .NET 8 SDK
- Docker (for local PostgreSQL)

### Frontend Setup

```bash
# Install dependencies
npm install

# Serve the React app
nx serve web

# Run tests
nx test web

# Build for production
nx build web
```

### Backend Setup

```bash
# Restore .NET packages
dotnet restore

# Run Aspire AppHost (starts API + DB)
dotnet run --project src/EvMarketplace.AppHost

# Run tests
dotnet test
```

## License

MIT
