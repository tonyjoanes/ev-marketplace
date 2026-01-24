#!/bin/bash

# EV Marketplace Infrastructure Deployment Script
# Usage: ./deploy.sh [dev|staging|prod]

set -e

# Check if environment parameter is provided
if [ -z "$1" ]; then
  echo "Error: Environment parameter required"
  echo "Usage: ./deploy.sh [dev|staging|prod]"
  exit 1
fi

ENVIRONMENT=$1
LOCATION="uksouth"
SUBSCRIPTION_ID="${AZURE_SUBSCRIPTION_ID:-}"

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
  echo "Error: Environment must be dev, staging, or prod"
  exit 1
fi

echo "================================================"
echo "EV Marketplace Infrastructure Deployment"
echo "================================================"
echo "Environment: $ENVIRONMENT"
echo "Location: $LOCATION"
echo "================================================"

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
  echo "Error: Azure CLI is not installed"
  echo "Install from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
  exit 1
fi

# Check if logged in to Azure
echo "Checking Azure login status..."
if ! az account show &> /dev/null; then
  echo "Not logged in to Azure. Please login..."
  az login
fi

# Set subscription if provided
if [ -n "$SUBSCRIPTION_ID" ]; then
  echo "Setting subscription to: $SUBSCRIPTION_ID"
  az account set --subscription "$SUBSCRIPTION_ID"
fi

# Display current subscription
CURRENT_SUB=$(az account show --query name -o tsv)
echo "Current subscription: $CURRENT_SUB"

# Confirm deployment
read -p "Do you want to proceed with deployment? (yes/no): " CONFIRM
if [[ ! "$CONFIRM" =~ ^(yes|y|Y)$ ]]; then
  echo "Deployment cancelled"
  exit 0
fi

# Run Bicep linting
echo ""
echo "Running Bicep lint..."
az bicep build --file ./bicep/main.bicep

# Validate deployment
echo ""
echo "Validating deployment..."
az deployment sub validate \
  --location "$LOCATION" \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.${ENVIRONMENT}.json

if [ $? -ne 0 ]; then
  echo "Error: Deployment validation failed"
  exit 1
fi

echo ""
echo "Validation successful!"

# What-if deployment (preview changes)
echo ""
echo "Running what-if analysis..."
az deployment sub what-if \
  --location "$LOCATION" \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.${ENVIRONMENT}.json

# Final confirmation
echo ""
read -p "Review the changes above. Proceed with actual deployment? (yes/no): " FINAL_CONFIRM
if [[ ! "$FINAL_CONFIRM" =~ ^(yes|y|Y)$ ]]; then
  echo "Deployment cancelled"
  exit 0
fi

# Deploy infrastructure
echo ""
echo "Deploying infrastructure..."
DEPLOYMENT_NAME="evmarket-${ENVIRONMENT}-$(date +%Y%m%d-%H%M%S)"

az deployment sub create \
  --name "$DEPLOYMENT_NAME" \
  --location "$LOCATION" \
  --template-file ./bicep/main.bicep \
  --parameters ./bicep/parameters.${ENVIRONMENT}.json \
  --verbose

if [ $? -eq 0 ]; then
  echo ""
  echo "================================================"
  echo "Deployment completed successfully!"
  echo "================================================"

  # Get deployment outputs
  echo ""
  echo "Retrieving deployment outputs..."
  az deployment sub show \
    --name "$DEPLOYMENT_NAME" \
    --query properties.outputs \
    --output table

  echo ""
  echo "Next steps:"
  echo "1. Update API app settings with database admin password"
  echo "2. Deploy application code using Azure DevOps pipelines"
  echo "3. Configure custom domain and SSL certificates (production only)"
  echo "4. Set up monitoring alerts in Azure Portal"
  echo "5. Update Action Group email addresses in monitoring.bicep"
else
  echo ""
  echo "Deployment failed. Check the error messages above."
  exit 1
fi
