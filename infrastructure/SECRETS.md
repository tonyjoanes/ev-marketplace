# Secrets and Configuration Management

This document outlines how to securely manage secrets, connection strings, and sensitive configuration for the EV Marketplace application.

## Overview

The application uses multiple types of secrets:
- Database credentials
- Storage account keys
- API keys (future integrations)
- Application Insights keys
- Function authorization keys

## Azure Key Vault Setup (Recommended for Production)

### 1. Create Key Vault

```bash
# Create Key Vault
az keyvault create \
  --name kv-evmarket-prod \
  --resource-group rg-evmarket-prod \
  --location uksouth \
  --enable-rbac-authorization false

# Enable soft delete and purge protection
az keyvault update \
  --name kv-evmarket-prod \
  --resource-group rg-evmarket-prod \
  --enable-purge-protection true
```

### 2. Store Secrets

```bash
# Database password
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name DatabasePassword \
  --value "your-secure-password-here"

# Storage account connection string
STORAGE_CONN=$(az storage account show-connection-string \
  --name stgevmarketprod \
  --resource-group rg-evmarket-prod \
  --query connectionString -o tsv)

az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name StorageConnectionString \
  --value "$STORAGE_CONN"

# Application Insights instrumentation key
APP_INSIGHTS_KEY=$(az monitor app-insights component show \
  --resource-group rg-evmarket-prod \
  --app appi-evmarket-prod \
  --query instrumentationKey -o tsv)

az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name AppInsightsInstrumentationKey \
  --value "$APP_INSIGHTS_KEY"
```

### 3. Grant Access to App Services

```bash
# Enable managed identity for API App
az webapp identity assign \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod \
  --query principalId -o tsv)

# Grant access to Key Vault
az keyvault set-policy \
  --name kv-evmarket-prod \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### 4. Reference Secrets in App Service

Update App Service configuration to reference Key Vault:

```bash
# Database password
az webapp config appsettings set \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod \
  --settings DatabasePassword="@Microsoft.KeyVault(SecretUri=https://kv-evmarket-prod.vault.azure.net/secrets/DatabasePassword/)"

# Build connection string with Key Vault reference
az webapp config connection-string set \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod \
  --connection-string-type PostgreSQL \
  --settings evmarketplace="Host=evmarket-db-prod.postgres.database.azure.com;Database=evmarketplace;Username=dbadmin;Password=@Microsoft.KeyVault(SecretUri=https://kv-evmarket-prod.vault.azure.net/secrets/DatabasePassword/)"
```

## Environment-Specific Configuration

### Development (.env.local)

For local development, create `.env.local` files (never commit to Git):

**Backend (.NET):**
```bash
# src/EvMarketplace.Api/.env.local
ASPNETCORE_ENVIRONMENT=Development
ConnectionStrings__evmarketplace="Host=localhost;Port=5432;Database=evmarketplace;Username=postgres;Password=your-local-password"
AzureStorage__ConnectionString="UseDevelopmentStorage=true"  # Use Azurite
ApplicationInsights__ConnectionString=""  # Leave empty for local dev
```

**Frontend (React):**
```bash
# apps/web/.env.local
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_INSIGHTS_INSTRUMENTATION_KEY=""
NODE_ENV=development
```

### Staging

Stored in Azure App Service Configuration (not Key Vault for easier testing):

```bash
# App Settings
ASPNETCORE_ENVIRONMENT=Staging
ApplicationInsights__ConnectionString=<from-app-insights>

# Connection Strings
ConnectionStrings__evmarketplace=<from-database>
AzureStorage__ConnectionString=<from-storage>
```

### Production

All secrets in Azure Key Vault with references in App Service:

```bash
# App Settings (Key Vault references)
ASPNETCORE_ENVIRONMENT=Production
ApplicationInsights__ConnectionString="@Microsoft.KeyVault(...)"
DatabasePassword="@Microsoft.KeyVault(...)"
StorageAccountKey="@Microsoft.KeyVault(...)"
```

## Database Credentials

### Generate Secure Password

```bash
# Generate strong password
openssl rand -base64 32
```

### PostgreSQL Configuration

```bash
# Set database admin password
az postgres flexible-server update \
  --resource-group rg-evmarket-prod \
  --name evmarket-db-prod \
  --admin-password "your-secure-password"

# Store in Key Vault
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name DatabasePassword \
  --value "your-secure-password"
```

### Connection String Format

```
Host=evmarket-db-prod.postgres.database.azure.com;Port=5432;Database=evmarketplace;Username=dbadmin;Password=<from-keyvault>;Ssl Mode=Require
```

## Storage Account Access

### Option 1: Connection String (Current)

```bash
# Get connection string
az storage account show-connection-string \
  --name stevmarketprod \
  --resource-group rg-evmarket-prod \
  --query connectionString -o tsv
```

### Option 2: Managed Identity (Recommended)

Update code to use `DefaultAzureCredential`:

```csharp
// In ImageProcessor Function
var credential = new DefaultAzureCredential();
var blobServiceClient = new BlobServiceClient(
    new Uri("https://stevmarketprod.blob.core.windows.net"),
    credential
);
```

Grant permissions:

```bash
# Get Function App identity
FUNC_PRINCIPAL_ID=$(az functionapp identity show \
  --resource-group rg-evmarket-prod \
  --name func-evmarket-prod \
  --query principalId -o tsv)

# Assign Storage Blob Data Contributor role
az role assignment create \
  --assignee $FUNC_PRINCIPAL_ID \
  --role "Storage Blob Data Contributor" \
  --scope "/subscriptions/$(az account show --query id -o tsv)/resourceGroups/rg-evmarket-prod/providers/Microsoft.Storage/storageAccounts/stevmarketprod"
```

## Function App Keys

### Get Function Keys

```bash
# Get master key (for admin operations)
az functionapp keys list \
  --resource-group rg-evmarket-prod \
  --name func-evmarket-prod

# Get function-specific key
az functionapp function keys list \
  --resource-group rg-evmarket-prod \
  --name func-evmarket-prod \
  --function-name ProcessVehicleImage
```

### Store Function Keys

```bash
# Store in Key Vault
FUNCTION_KEY=$(az functionapp function keys list \
  --resource-group rg-evmarket-prod \
  --name func-evmarket-prod \
  --function-name ProcessVehicleImage \
  --query default -o tsv)

az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name ImageProcessorFunctionKey \
  --value "$FUNCTION_KEY"
```

## Future API Keys

### Zap-Map API

```bash
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name ZapMapApiKey \
  --value "your-api-key"
```

### Auto Trader API

```bash
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name AutoTraderApiKey \
  --value "your-api-key"
```

### Octopus Energy API

```bash
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name OctopusEnergyApiKey \
  --value "your-api-key"
```

## CI/CD Pipeline Secrets

Store in Azure DevOps Library or GitHub Secrets:

### Azure DevOps Variable Groups

```yaml
# In Azure DevOps → Pipelines → Library
Variable Group: evmarket-prod-secrets
Variables:
  - AZURE_SUBSCRIPTION_ID: <subscription-id>
  - AZURE_TENANT_ID: <tenant-id>
  - KEY_VAULT_NAME: kv-evmarket-prod
  - RESOURCE_GROUP: rg-evmarket-prod
```

### GitHub Secrets

```bash
# Set repository secrets
gh secret set AZURE_CREDENTIALS --body '{
  "clientId": "<client-id>",
  "clientSecret": "<client-secret>",
  "subscriptionId": "<subscription-id>",
  "tenantId": "<tenant-id>"
}'

gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription-id>"
```

## Rotation Policy

### Database Credentials

Rotate every 90 days:

```bash
# Generate new password
NEW_PASSWORD=$(openssl rand -base64 32)

# Update database
az postgres flexible-server update \
  --resource-group rg-evmarket-prod \
  --name evmarket-db-prod \
  --admin-password "$NEW_PASSWORD"

# Update Key Vault
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name DatabasePassword \
  --value "$NEW_PASSWORD"

# Restart App Service to pick up new secret
az webapp restart \
  --resource-group rg-evmarket-prod \
  --name app-evmarket-api-prod
```

### Storage Account Keys

Rotate every 180 days:

```bash
# Regenerate key2
az storage account keys renew \
  --resource-group rg-evmarket-prod \
  --account-name stevmarketprod \
  --key key2

# Get new connection string
NEW_CONN=$(az storage account show-connection-string \
  --name stevmarketprod \
  --resource-group rg-evmarket-prod \
  --key key2 \
  --query connectionString -o tsv)

# Update Key Vault
az keyvault secret set \
  --vault-name kv-evmarket-prod \
  --name StorageConnectionString \
  --value "$NEW_CONN"
```

## Security Best Practices

1. ✅ **Never commit secrets to Git**
   - Use `.gitignore` for `.env.local` files
   - Use Key Vault references in App Service
   - Scan repositories with tools like GitGuardian

2. ✅ **Use Managed Identities**
   - Preferred over connection strings
   - Eliminates credential management
   - Automatic rotation by Azure

3. ✅ **Implement Least Privilege**
   - Grant minimum required permissions
   - Use separate identities for each service
   - Review permissions quarterly

4. ✅ **Enable Audit Logging**
   ```bash
   # Enable Key Vault diagnostics
   az monitor diagnostic-settings create \
     --resource $(az keyvault show --name kv-evmarket-prod --query id -o tsv) \
     --name kv-diagnostics \
     --workspace $(az monitor log-analytics workspace show --resource-group rg-evmarket-prod --workspace-name log-evmarket-prod --query id -o tsv) \
     --logs '[{"category": "AuditEvent", "enabled": true}]'
   ```

5. ✅ **Regular Rotation**
   - Database: 90 days
   - Storage keys: 180 days
   - Function keys: 180 days
   - API keys: As per vendor requirements

6. ✅ **Monitor Access**
   - Set up alerts for Key Vault access
   - Review access logs monthly
   - Investigate suspicious activity

## Emergency Procedures

### Compromised Database Credentials

```bash
# 1. Immediately rotate password
NEW_PASSWORD=$(openssl rand -base64 32)
az postgres flexible-server update --admin-password "$NEW_PASSWORD"

# 2. Update Key Vault
az keyvault secret set --name DatabasePassword --value "$NEW_PASSWORD"

# 3. Restart all services
az webapp restart --name app-evmarket-api-prod
az functionapp restart --name func-evmarket-prod

# 4. Audit database access logs
az postgres flexible-server server-logs list \
  --resource-group rg-evmarket-prod \
  --name evmarket-db-prod
```

### Compromised Storage Keys

```bash
# 1. Regenerate both keys (requires downtime)
az storage account keys renew --key key1
az storage account keys renew --key key2

# 2. Update Key Vault
# 3. Restart services
# 4. Review blob access logs
```

## Compliance and Auditing

### Key Vault Access Report

```bash
# Query Key Vault audit logs
az monitor log-analytics query \
  --workspace $(az monitor log-analytics workspace show --resource-group rg-evmarket-prod --workspace-name log-evmarket-prod --query customerId -o tsv) \
  --analytics-query "AzureDiagnostics | where ResourceType == 'VAULTS' | project TimeGenerated, OperationName, CallerIPAddress, ResultType" \
  --timespan P30D
```

### Secret Version History

```bash
# List all versions of a secret
az keyvault secret list-versions \
  --vault-name kv-evmarket-prod \
  --name DatabasePassword
```

## Development Team Access

### Grant Developer Access to Key Vault (Read-Only)

```bash
# Get developer's object ID
DEV_OBJECT_ID=$(az ad user show --id developer@company.com --query id -o tsv)

# Grant read-only access
az keyvault set-policy \
  --name kv-evmarket-prod \
  --object-id $DEV_OBJECT_ID \
  --secret-permissions get list
```

### Local Development Setup

Developers should use Azure CLI to fetch secrets locally:

```bash
# Login to Azure
az login

# Fetch database password
az keyvault secret show \
  --vault-name kv-evmarket-prod \
  --name DatabasePassword \
  --query value -o tsv

# Or use script to populate .env.local
./scripts/fetch-secrets.sh
```

## Summary

| Secret Type | Storage | Access Method | Rotation |
|-------------|---------|---------------|----------|
| Database Password | Key Vault | Managed Identity + Key Vault Ref | 90 days |
| Storage Keys | Key Vault | Managed Identity (preferred) | 180 days |
| Function Keys | Key Vault | Service-to-service calls | 180 days |
| API Keys | Key Vault | Managed Identity + Key Vault Ref | As per vendor |
| App Insights | Key Vault | Connection String | N/A (managed) |

Use Managed Identities wherever possible to eliminate credential management entirely.
