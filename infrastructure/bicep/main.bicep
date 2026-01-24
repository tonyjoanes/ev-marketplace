// Main infrastructure orchestration for EV Marketplace
targetScope = 'subscription'

@description('Environment name (e.g., dev, staging, prod)')
@allowed(['dev', 'staging', 'prod'])
param environment string

@description('Azure region for resources')
param location string = 'uksouth'

@description('Resource group name')
param resourceGroupName string = 'rg-evmarket-${environment}'

@description('Application name')
param appName string = 'evmarket'

// Common tags for all resources
var commonTags = {
  environment: environment
  application: appName
  managedBy: 'bicep'
  createdDate: utcNow('yyyy-MM-dd')
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: commonTags
}

// Monitoring (deploy first to get instrumentation keys)
module monitoring 'monitoring.bicep' = {
  name: 'monitoring-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    tags: commonTags
  }
}

// PostgreSQL Database
module database 'database.bicep' = {
  name: 'database-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    tags: commonTags
  }
}

// Storage Account for vehicle images
module storage 'storage.bicep' = {
  name: 'storage-deployment'
  scope: rg
  params: {
    location: location
    environment: environment
    tags: commonTags
  }
}

// App Service Plan and Web Apps
module appService 'app-service.bicep' = {
  name: 'appservice-deployment'
  scope: rg
  dependsOn: [
    database
    storage
    monitoring
  ]
  params: {
    location: location
    environment: environment
    tags: commonTags
    databaseConnectionString: database.outputs.connectionString
    storageConnectionString: storage.outputs.connectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

// Function Apps for background processing
module functions 'functions.bicep' = {
  name: 'functions-deployment'
  scope: rg
  dependsOn: [
    storage
    monitoring
  ]
  params: {
    location: location
    environment: environment
    tags: commonTags
    storageConnectionString: storage.outputs.connectionString
    appInsightsInstrumentationKey: monitoring.outputs.appInsightsInstrumentationKey
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
  }
}

// Outputs
output resourceGroupName string = rg.name
output location string = location
output environment string = environment

// Database outputs
output databaseServerName string = database.outputs.serverName
output databaseName string = database.outputs.databaseName

// Storage outputs
output storageAccountName string = storage.outputs.storageAccountName
output storageAccountId string = storage.outputs.storageAccountId

// App Service outputs
output apiAppName string = appService.outputs.apiAppName
output apiAppUrl string = appService.outputs.apiAppUrl
output webAppName string = appService.outputs.webAppName
output webAppUrl string = appService.outputs.webAppUrl

// Function outputs
output functionAppName string = functions.outputs.functionAppName
output functionAppUrl string = functions.outputs.functionAppUrl

// Monitoring outputs
output appInsightsName string = monitoring.outputs.appInsightsName
output appInsightsInstrumentationKey string = monitoring.outputs.appInsightsInstrumentationKey
output logAnalyticsWorkspaceId string = monitoring.outputs.logAnalyticsWorkspaceId
