# EV Marketplace Infrastructure Deployment Script (PowerShell)
# Usage: .\deploy.ps1 -Environment [dev|staging|prod]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev','staging','prod')]
    [string]$Environment,

    [Parameter(Mandatory=$false)]
    [string]$Location = "uksouth",

    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId = $env:AZURE_SUBSCRIPTION_ID
)

$ErrorActionPreference = "Stop"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "EV Marketplace Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "================================================" -ForegroundColor Cyan

# Check if Azure CLI is installed
try {
    $azVersion = az --version
    if (-not $azVersion) {
        throw "Azure CLI not found"
    }
}
catch {
    Write-Host "Error: Azure CLI is not installed" -ForegroundColor Red
    Write-Host "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli" -ForegroundColor Yellow
    exit 1
}

# Check if logged in to Azure
Write-Host "`nChecking Azure login status..." -ForegroundColor Green
try {
    $account = az account show | ConvertFrom-Json
    if (-not $account) {
        throw "Not logged in"
    }
}
catch {
    Write-Host "Not logged in to Azure. Please login..." -ForegroundColor Yellow
    az login
}

# Set subscription if provided
if ($SubscriptionId) {
    Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Green
    az account set --subscription $SubscriptionId
}

# Display current subscription
$currentSub = az account show --query name -o tsv
Write-Host "Current subscription: $currentSub" -ForegroundColor Green

# Confirm deployment
$confirm = Read-Host "`nDo you want to proceed with deployment? (yes/no)"
if ($confirm -notin @('yes', 'y', 'Y')) {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

# Run Bicep linting
Write-Host "`nRunning Bicep lint..." -ForegroundColor Green
az bicep build --file ./bicep/main.bicep

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Bicep lint failed" -ForegroundColor Red
    exit 1
}

# Validate deployment
Write-Host "`nValidating deployment..." -ForegroundColor Green
az deployment sub validate `
    --location $Location `
    --template-file ./bicep/main.bicep `
    --parameters ./bicep/parameters.$Environment.json

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Deployment validation failed" -ForegroundColor Red
    exit 1
}

Write-Host "`nValidation successful!" -ForegroundColor Green

# What-if deployment (preview changes)
Write-Host "`nRunning what-if analysis..." -ForegroundColor Green
az deployment sub what-if `
    --location $Location `
    --template-file ./bicep/main.bicep `
    --parameters ./bicep/parameters.$Environment.json

# Final confirmation
$finalConfirm = Read-Host "`nReview the changes above. Proceed with actual deployment? (yes/no)"
if ($finalConfirm -notin @('yes', 'y', 'Y')) {
    Write-Host "Deployment cancelled" -ForegroundColor Yellow
    exit 0
}

# Deploy infrastructure
Write-Host "`nDeploying infrastructure..." -ForegroundColor Green
$deploymentName = "evmarket-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

az deployment sub create `
    --name $deploymentName `
    --location $Location `
    --template-file ./bicep/main.bicep `
    --parameters ./bicep/parameters.$Environment.json `
    --verbose

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n================================================" -ForegroundColor Green
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green

    # Get deployment outputs
    Write-Host "`nRetrieving deployment outputs..." -ForegroundColor Green
    az deployment sub show `
        --name $deploymentName `
        --query properties.outputs `
        --output table

    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Update API app settings with database admin password" -ForegroundColor Yellow
    Write-Host "2. Deploy application code using Azure DevOps pipelines" -ForegroundColor Yellow
    Write-Host "3. Configure custom domain and SSL certificates (production only)" -ForegroundColor Yellow
    Write-Host "4. Set up monitoring alerts in Azure Portal" -ForegroundColor Yellow
    Write-Host "5. Update Action Group email addresses in monitoring.bicep" -ForegroundColor Yellow
}
else {
    Write-Host "`nDeployment failed. Check the error messages above." -ForegroundColor Red
    exit 1
}
