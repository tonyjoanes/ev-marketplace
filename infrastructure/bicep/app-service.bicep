// Azure App Service Plan and Web Apps for API and Frontend
param location string
param environment string
param tags object
param databaseConnectionString string
param storageConnectionString string
param appInsightsInstrumentationKey string
param appInsightsConnectionString string

var appServicePlanName = 'asp-evmarket-${environment}'
var apiAppName = 'app-evmarket-api-${environment}-${uniqueString(resourceGroup().id)}'
var webAppName = 'app-evmarket-web-${environment}-${uniqueString(resourceGroup().id)}'

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: environment == 'prod' ? 'P1v3' : 'B1'
    tier: environment == 'prod' ? 'PremiumV3' : 'Basic'
    capacity: environment == 'prod' ? 2 : 1
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
    zoneRedundant: environment == 'prod'
  }
}

// API App Service (.NET 8)
resource apiApp 'Microsoft.Web/sites@2023-01-01' = {
  name: apiAppName
  location: location
  tags: union(tags, { 'app-type': 'api' })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      cors: {
        allowedOrigins: [
          'https://${webAppName}.azurewebsites.net'
          environment == 'dev' ? 'http://localhost:4200' : ''
          environment == 'dev' ? 'http://localhost:5173' : ''
        ]
        supportCredentials: false
      }
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environment == 'prod' ? 'Production' : 'Development'
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
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
        {
          name: 'AzureStorage__ConnectionString'
          value: storageConnectionString
        }
      ]
      connectionStrings: [
        {
          name: 'evmarketplace'
          connectionString: databaseConnectionString
          type: 'PostgreSQL'
        }
      ]
    }
  }
}

// API App - Staging slot for blue-green deployment
resource apiAppStaging 'Microsoft.Web/sites/slots@2023-01-01' = if (environment == 'prod') {
  parent: apiApp
  name: 'staging'
  location: location
  tags: union(tags, { 'app-type': 'api', slot: 'staging' })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
      ]
      connectionStrings: [
        {
          name: 'evmarketplace'
          connectionString: databaseConnectionString
          type: 'PostgreSQL'
        }
      ]
    }
  }
}

// Frontend App Service (Static Web App alternative)
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: webAppName
  location: location
  tags: union(tags, { 'app-type': 'frontend' })
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appCommandLine: 'node server.js' // Assuming a simple Node.js server for the React build
      appSettings: [
        {
          name: 'NODE_ENV'
          value: environment == 'prod' ? 'production' : 'development'
        }
        {
          name: 'VITE_API_BASE_URL'
          value: 'https://${apiAppName}.azurewebsites.net'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Auto-scaling for production
resource autoScale 'Microsoft.Insights/autoscalesettings@2022-10-01' = if (environment == 'prod') {
  name: 'autoscale-${appServicePlanName}'
  location: location
  tags: tags
  properties: {
    enabled: true
    targetResourceUri: appServicePlan.id
    profiles: [
      {
        name: 'Auto scale based on CPU'
        capacity: {
          minimum: '2'
          maximum: '10'
          default: '2'
        }
        rules: [
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'GreaterThan'
              threshold: 70
            }
            scaleAction: {
              direction: 'Increase'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT5M'
            }
          }
          {
            metricTrigger: {
              metricName: 'CpuPercentage'
              metricResourceUri: appServicePlan.id
              timeGrain: 'PT1M'
              statistic: 'Average'
              timeWindow: 'PT5M'
              timeAggregation: 'Average'
              operator: 'LessThan'
              threshold: 30
            }
            scaleAction: {
              direction: 'Decrease'
              type: 'ChangeCount'
              value: '1'
              cooldown: 'PT10M'
            }
          }
        ]
      }
    ]
  }
}

output apiAppName string = apiApp.name
output apiAppUrl string = 'https://${apiApp.properties.defaultHostName}'
output apiAppId string = apiApp.id
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output webAppId string = webApp.id
output appServicePlanId string = appServicePlan.id
