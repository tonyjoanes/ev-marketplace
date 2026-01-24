# Infrastructure Documentation

This folder contains Azure infrastructure as code (IaC) and CI/CD pipeline definitions for the EV Marketplace platform.

## Structure

```
infrastructure/
├── azure-pipelines/     # Azure DevOps pipeline YAML files
│   ├── ci-backend.yml   # Backend build and test pipeline
│   ├── ci-frontend.yml  # Frontend build and test pipeline
│   ├── cd-staging.yml   # Deploy to staging environment
│   └── cd-production.yml # Deploy to production environment
├── bicep/              # Azure Bicep templates for infrastructure
│   ├── main.bicep      # Main infrastructure orchestration
│   ├── app-service.bicep # Web app and API hosting
│   ├── database.bicep  # PostgreSQL database
│   ├── storage.bicep   # Blob storage for images
│   └── functions.bicep # Function apps
└── functions/          # Azure Functions code
    ├── ImageProcessor/ # Image processing function
    ├── EmailNotifier/  # Email notification function
    └── ListingCleanup/ # Scheduled cleanup function
```

## Azure Resources

### Production Environment
- **Resource Group**: `rg-evmarket-prod`
- **App Service Plan**: Linux B1 or higher
- **Web App**: React frontend (static files)
- **API App**: .NET 8 Web API
- **Database**: Azure Database for PostgreSQL Flexible Server
- **Storage Account**: Blob storage for vehicle images
- **Function App**: Background processing (Consumption plan)
- **Application Insights**: Monitoring and telemetry
- **Key Vault**: Secrets management

### Staging Environment
- **Resource Group**: `rg-evmarket-staging`
- Mirror of production with lower-tier resources

## Azure Function Use Cases

### 1. Image Processor (HTTP + Queue Trigger)
**Purpose**: Process uploaded vehicle images
- Resize to multiple sizes (thumbnail, card, full)
- Convert to WebP format
- Generate responsive image sets
- Store in Azure Blob Storage
- Update listing with image URLs

**Trigger**: HTTP POST from API or Queue message

### 2. Email Notifier (Queue Trigger)
**Purpose**: Send transactional emails
- New listing confirmation
- Listing approved/rejected
- Price change alerts
- Weekly digest

**Trigger**: Azure Queue Storage message

### 3. Listing Cleanup (Timer Trigger)
**Purpose**: Maintain data hygiene
- Mark 60-day old listings as expired
- Clean up orphaned images
- Generate analytics reports

**Trigger**: CRON schedule (daily at 2 AM)

### 4. DVLA Verification (Queue Trigger)
**Purpose**: Verify UK vehicle registrations
- Call DVLA Vehicle Enquiry API
- Validate vehicle details
- Flag suspicious listings

**Trigger**: Queue message when listing created

### 5. Price Alert Checker (Timer Trigger)
**Purpose**: Notify users of price drops
- Check watchlisted vehicles
- Compare current vs saved price
- Send notification if price dropped

**Trigger**: CRON schedule (hourly)

## CI/CD Pipeline Flow

### Backend Pipeline
1. **Trigger**: PR to main or direct push
2. **Restore**: `dotnet restore`
3. **Build**: `dotnet build --configuration Release`
4. **Test**: `dotnet test` with code coverage
5. **Publish**: Create deployment artifact
6. **Deploy to Staging**: Auto-deploy on PR merge
7. **Deploy to Production**: Manual approval required

### Frontend Pipeline
1. **Trigger**: PR to main or direct push
2. **Install**: `npm install`
3. **Lint**: `nx lint web`
4. **Test**: `nx test web`
5. **Build**: `nx build web --configuration=production`
6. **Deploy to Staging**: Upload to Azure Static Web Apps
7. **Deploy to Production**: Manual approval required

## Environment Variables

### Backend API
```
ConnectionStrings__DefaultConnection=<PostgreSQL connection>
ApplicationInsights__ConnectionString=<App Insights>
AzureStorage__ConnectionString=<Storage account>
AzureStorage__ContainerName=vehicle-images
EmailService__SendGridApiKey=<SendGrid key>
DvlaApi__BaseUrl=https://driver-vehicle-licensing.api.gov.uk
DvlaApi__ApiKey=<DVLA API key>
```

### Frontend
```
VITE_API_URL=https://api.evmarket.co.uk
VITE_ENVIRONMENT=production
```

### Functions
```
AzureWebJobsStorage=<Storage connection>
FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
DatabaseConnection=<PostgreSQL connection>
SendGridApiKey=<SendGrid key>
ImageStorage__ConnectionString=<Storage connection>
ImageStorage__ContainerName=vehicle-images
```

## Cost Estimation (Monthly)

**Staging**:
- App Service B1: £10
- PostgreSQL Burstable B1ms: £12
- Storage (100 GB): £2
- Function Consumption: £1
- **Total**: ~£25/month

**Production** (low traffic):
- App Service S1: £50
- PostgreSQL General Purpose D2s: £100
- Storage (500 GB): £10
- Function Consumption: £5
- Application Insights: £10
- **Total**: ~£175/month

**Production** (high traffic):
- App Service P1v2: £140
- PostgreSQL General Purpose D4s: £200
- Storage (2 TB): £40
- Functions Premium: £120
- Application Insights: £50
- CDN: £30
- **Total**: ~£580/month

## Deployment Steps

### Initial Setup
1. Create service principal for Azure DevOps
2. Configure service connections in Azure DevOps
3. Create resource groups
4. Deploy infrastructure with Bicep
5. Configure custom domains and SSL
6. Set up Azure DevOps pipelines
7. Configure branch policies

### Continuous Deployment
1. Push to `main` branch
2. Pipeline triggers automatically
3. Builds and tests code
4. Deploys to staging environment
5. Run smoke tests
6. Await manual approval for production
7. Deploy to production
8. Monitor Application Insights

## Monitoring & Alerts

- **Application Insights**: Request telemetry, exceptions, dependencies
- **Availability Tests**: Ping API and frontend every 5 minutes
- **Alerts**:
  - API response time > 1s
  - Error rate > 5%
  - Database CPU > 80%
  - Function failures
  - Storage capacity > 90%

## Security

- All secrets in Azure Key Vault
- Managed Identity for service-to-service auth
- HTTPS only
- CORS restricted to known domains
- SQL connection with SSL required
- Regular security scans in pipeline
- Dependency vulnerability checks

## Backup & Recovery

- **Database**: Automated daily backups, 7-day retention
- **Blob Storage**: Geo-redundant storage (GRS)
- **Configuration**: All IaC in Git
- **RTO**: < 4 hours
- **RPO**: < 24 hours

## Next Steps

1. Create Azure DevOps project
2. Set up service connections
3. Review and customize Bicep templates
4. Configure environment-specific variables
5. Set up branch policies
6. Test deployment to staging
7. Configure custom domain
8. Set up monitoring alerts
