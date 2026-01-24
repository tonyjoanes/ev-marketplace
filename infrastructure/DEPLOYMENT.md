# EV Marketplace - Deployment Guide

This guide covers deploying the EV Marketplace infrastructure to Azure using Bicep templates.

## Prerequisites

Before deploying, ensure you have:

1. **Azure CLI** installed (version 2.50.0 or later)
   ```bash
   az --version
   ```
   Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli

2. **Bicep** installed (comes with Azure CLI)
   ```bash
   az bicep version
   ```

3. **Azure Subscription** with appropriate permissions:
   - Owner or Contributor role at subscription level
   - Ability to create resource groups and resources

4. **Environment Variables** (optional but recommended):
   ```bash
   export AZURE_SUBSCRIPTION_ID="your-subscription-id"
   ```

## Infrastructure Overview

The deployment creates the following Azure resources:

| Resource | Purpose | SKU/Tier |
|----------|---------|----------|
| Resource Group | Container for all resources | - |
| PostgreSQL Flexible Server | Database for vehicle listings | Burstable B1ms (dev), General Purpose (prod) |
| Storage Account | Vehicle images and blobs | Standard_LRS (dev), Standard_ZRS (prod) |
| App Service Plan | Hosting for API and frontend | Basic B1 (dev), Premium P1v3 (prod) |
| App Service (API) | .NET 8 API backend | Linux, .NET 8 |
| App Service (Web) | React frontend | Linux, Node.js 20 |
| Function App | Image processing | Consumption (dev), Premium EP1 (prod) |
| Application Insights | Monitoring and diagnostics | - |
| Log Analytics Workspace | Centralized logging | - |

## Deployment Steps

### 1. Login to Azure

```bash
az login
```

### 2. Set Subscription (if you have multiple)

```bash
az account set --subscription "your-subscription-id"
az account show
```

### 3. Review Parameters

Edit the appropriate parameter file for your environment:
- `bicep/parameters.dev.json` - Development
- `bicep/parameters.staging.json` - Staging
- `bicep/parameters.prod.json` - Production

```json
{
  "environment": "dev",
  "location": "uksouth",
  "resourceGroupName": "rg-evmarket-dev",
  "appName": "evmarket"
}
```

### 4. Deploy Infrastructure

#### Option A: Using Deployment Scripts (Recommended)

**Linux/macOS:**
```bash
cd infrastructure
./deploy.sh dev
```

**Windows (PowerShell):**
```powershell
cd infrastructure
.\deploy.ps1 -Environment dev
```

The script will:
1. ✅ Validate you're logged in to Azure
2. ✅ Run Bicep linting
3. ✅ Validate the deployment template
4. ✅ Show what-if analysis (preview changes)
5. ✅ Deploy all resources
6. ✅ Display deployment outputs

#### Option B: Manual Deployment

```bash
# Navigate to infrastructure directory
cd infrastructure

# Validate the template
az deployment sub validate \
  --location uksouth \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.dev.json

# Preview changes (what-if)
az deployment sub what-if \
  --location uksouth \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.dev.json

# Deploy
az deployment sub create \
  --name evmarket-dev-20260124 \
  --location uksouth \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.dev.json
```

### 5. Retrieve Deployment Outputs

After successful deployment:

```bash
# Get all outputs
az deployment sub show \
  --name evmarket-dev-20260124 \
  --query properties.outputs \
  --output table

# Get specific output (e.g., API URL)
az deployment sub show \
  --name evmarket-dev-20260124 \
  --query properties.outputs.apiAppUrl.value \
  --output tsv
```

## Post-Deployment Configuration

### 1. Database Setup

The database is created with migrations applied automatically in development. For production:

```bash
# Get connection string
DB_SERVER=$(az postgres flexible-server show --resource-group rg-evmarket-prod --name evmarket-db-prod --query fullyQualifiedDomainName -o tsv)

# Update connection string in App Service
az webapp config connection-string set \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod \
  --connection-string-type PostgreSQL \
  --settings evmarketplace="Host=$DB_SERVER;Database=evmarketplace;Username=dbadmin;Password=YOUR_PASSWORD"
```

### 2. Storage Configuration

Create a SAS token for Function App access:

```bash
# Get storage account name
STORAGE_ACCOUNT=$(az deployment sub show --name evmarket-prod-20260124 --query properties.outputs.storageAccountName.value -o tsv)

# Generate SAS token (valid for 1 year)
END_DATE=$(date -u -d "1 year" '+%Y-%m-%dT%H:%MZ')
SAS_TOKEN=$(az storage account generate-sas \
  --account-name $STORAGE_ACCOUNT \
  --services b \
  --resource-types sco \
  --permissions rwdlac \
  --expiry $END_DATE \
  --https-only \
  --output tsv)

echo "SAS Token: $SAS_TOKEN"
```

### 3. Application Insights

Get instrumentation key:

```bash
az monitor app-insights component show \
  --resource-group rg-evmarket-prod \
  --app appi-evmarket-prod \
  --query instrumentationKey \
  --output tsv
```

### 4. Function App Configuration

Deploy the ImageProcessor function:

```bash
cd ../infrastructure/functions/ImageProcessor

# Build the function
dotnet build --configuration Release

# Publish
dotnet publish --configuration Release --output ./publish

# Create deployment package
cd publish
zip -r ../ImageProcessor.zip .
cd ..

# Deploy to Azure
az functionapp deployment source config-zip \
  --resource-group rg-evmarket-prod \
  --name func-evmarket-prod \
  --src ImageProcessor.zip
```

### 5. Custom Domain and SSL (Production Only)

```bash
# Add custom domain
az webapp config hostname add \
  --resource-group rg-evmarket-prod \
  --webapp-name app-evmarket-web-prod \
  --hostname www.evmarketplace.com

# Enable managed certificate
az webapp config ssl create \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-web-prod \
  --hostname www.evmarketplace.com
```

## Environment-Specific Configurations

### Development
- Single instance
- Burstable database tier
- Basic App Service plan
- Minimal logging retention (30 days)
- CORS allows localhost

### Staging
- Zone-redundant storage
- General Purpose database
- Standard App Service plan
- Deployment slots enabled
- 60 days retention

### Production
- Zone-redundant resources
- High availability database
- Premium App Service plan
- Auto-scaling enabled
- Deployment slots (blue/green)
- Availability tests
- 90 days retention
- WAF enabled (future)

## Monitoring and Alerts

After deployment, configure monitoring:

1. **Application Insights Dashboard**
   - Navigate to Azure Portal → Application Insights → evmarket-ai-prod
   - Pin key metrics to dashboard

2. **Alert Action Groups**
   - Update email address in `bicep/monitoring.bicep`
   - Redeploy monitoring module only:
     ```bash
     az deployment group create \
       --resource-group rg-evmarket-prod \
       --template-file ./bicep/monitoring.bicep \
       --parameters environment=prod location=uksouth
     ```

3. **Log Queries**
   - Failed requests: `requests | where success == false`
   - Slow queries: `requests | where duration > 2000`
   - Function errors: `traces | where severityLevel >= 3`

## Cost Management

Monitor costs in Azure Portal:

```bash
# Get cost analysis
az consumption usage list \
  --start-date 2026-01-01 \
  --end-date 2026-01-31 \
  --query "[?contains(resourceGroup, 'evmarket')].{Resource:instanceName, Cost:pretaxCost}" \
  --output table
```

Set up budget alerts:
- Development: £50/month
- Staging: £100/month
- Production: £500/month

## Troubleshooting

### Deployment Fails

1. **Check validation errors:**
   ```bash
   az deployment sub validate \
     --location uksouth \
     --template-file ./bicep/main.bicep \
     --parameters ./bicep/parameters.dev.json
   ```

2. **Review activity log:**
   ```bash
   az monitor activity-log list \
     --resource-group rg-evmarket-dev \
     --max-events 50 \
     --query "[].{Time:eventTimestamp, Status:status.value, Operation:operationName.value}" \
     --output table
   ```

3. **Common issues:**
   - **Region capacity**: Try different Azure region
   - **Quota limits**: Request quota increase
   - **Naming conflicts**: Ensure globally unique names

### App Service Not Starting

1. **Check logs:**
   ```bash
   az webapp log tail \
     --resource-group rg-evmarket-dev \
     --name app-evmarket-api-dev
   ```

2. **Verify connection strings** in Configuration → Connection Strings

3. **Check Application Insights** for startup errors

### Database Connection Issues

1. **Verify firewall rules:**
   ```bash
   az postgres flexible-server firewall-rule list \
     --resource-group rg-evmarket-dev \
     --name evmarket-db-dev
   ```

2. **Test connection:**
   ```bash
   psql "host=evmarket-db-dev.postgres.database.azure.com port=5432 dbname=evmarketplace user=dbadmin password=YOUR_PASSWORD sslmode=require"
   ```

## Cleanup

To delete all resources:

```bash
# Delete resource group (removes all resources)
az group delete --name rg-evmarket-dev --yes --no-wait

# Verify deletion
az group exists --name rg-evmarket-dev
```

## Security Best Practices

1. ✅ Use managed identities instead of connection strings where possible
2. ✅ Store secrets in Azure Key Vault
3. ✅ Enable database firewall rules
4. ✅ Use private endpoints for production
5. ✅ Enable Microsoft Defender for Cloud
6. ✅ Implement least privilege access (RBAC)
7. ✅ Regularly rotate credentials
8. ✅ Enable diagnostic logging
9. ✅ Use Application Gateway with WAF (production)
10. ✅ Implement rate limiting on APIs

## Next Steps

After successful deployment:

1. Configure CI/CD pipelines in Azure DevOps
2. Deploy application code
3. Set up monitoring dashboards
4. Configure backup and disaster recovery
5. Implement authentication (Azure AD B2C)
6. Set up CDN for frontend (production)

## Support

For issues or questions:
- Azure Documentation: https://docs.microsoft.com/azure
- Bicep Documentation: https://docs.microsoft.com/azure/azure-resource-manager/bicep
- Project Repository: https://github.com/tonyjoanes/ev-marketplace
