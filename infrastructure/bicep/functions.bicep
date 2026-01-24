// Azure Function App for serverless functions
param location string
param environment string
param tags object
param storageConnectionString string
param appInsightsInstrumentationKey string
param appInsightsConnectionString string

var functionAppName = 'func-evmarket-${environment}-${uniqueString(resourceGroup().id)}'
var hostingPlanName = 'asp-func-evmarket-${environment}'
var storageAccountNameForFunctions = 'stfunc${environment}${uniqueString(resourceGroup().id)}'

// Storage account for Azure Functions (required for function app state)
resource functionStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountNameForFunctions
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

// Consumption Plan for Functions (pay-per-execution)
// For production, consider Premium plan for better performance
resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: hostingPlanName
  location: location
  tags: tags
  sku: {
    // Consumption plan for cost-effective serverless
    // For production with predictable load, consider:
    // name: 'EP1' (Elastic Premium)
    // tier: 'ElasticPremium'
    name: environment == 'prod' ? 'EP1' : 'Y1'
    tier: environment == 'prod' ? 'ElasticPremium' : 'Dynamic'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
    maximumElasticWorkerCount: environment == 'prod' ? 20 : 1
  }
}

// Function App
resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  tags: union(tags, { 'function-type': 'image-processing' })
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      alwaysOn: environment == 'prod' // Only available on Premium plans
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      cors: {
        allowedOrigins: [
          '*' // Adjust based on your security requirements
        ]
      }
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(functionAppName)
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
        }
        // Storage connection for vehicle images
        {
          name: 'VehicleImagesStorage'
          value: storageConnectionString
        }
        // Queue connection for image processing
        {
          name: 'ImageProcessingQueue'
          value: storageConnectionString
        }
        // Enable detailed logging for development
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'SCALE_CONTROLLER_LOGGING_ENABLED'
          value: environment == 'dev' ? 'AppInsights:Verbose' : 'AppInsights:Information'
        }
      ]
    }
  }
}

// Function App - Staging slot for production
resource functionAppStaging 'Microsoft.Web/sites/slots@2023-01-01' = if (environment == 'prod') {
  parent: functionApp
  name: 'staging'
  location: location
  tags: union(tags, { 'function-type': 'image-processing', slot: 'staging' })
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|8.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${functionStorageAccount.name};AccountKey=${functionStorageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'VehicleImagesStorage'
          value: storageConnectionString
        }
      ]
    }
  }
}

// Diagnostic settings for function monitoring
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: functionApp
  name: 'function-diagnostics'
  properties: {
    logs: [
      {
        category: 'FunctionAppLogs'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: environment == 'prod' ? 90 : 30
        }
      }
    ]
  }
}

output functionAppName string = functionApp.name
output functionAppId string = functionApp.id
output functionAppUrl string = 'https://${functionApp.properties.defaultHostName}'
output functionAppPrincipalId string = functionApp.identity.principalId
output hostingPlanId string = hostingPlan.id
