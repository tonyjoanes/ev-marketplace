// Application Insights and monitoring infrastructure
param location string
param environment string
param tags object

var appInsightsName = 'appi-evmarket-${environment}'
var logAnalyticsWorkspaceName = 'log-evmarket-${environment}'
var actionGroupName = 'ag-evmarket-${environment}'
var alertPrefix = 'alert-evmarket-${environment}'

// Log Analytics Workspace (required for Application Insights)
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: environment == 'prod' ? 90 : 30
    features: {
      enableLogAccessUsingOnlyResourcePermissions: true
    }
    workspaceCapping: {
      dailyQuotaGb: environment == 'prod' ? 10 : 1
    }
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    RetentionInDays: environment == 'prod' ? 90 : 30
  }
}

// Action Group for alerts (email notifications)
resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: actionGroupName
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'EVMarket'
    enabled: true
    emailReceivers: [
      {
        name: 'Admin Email'
        emailAddress: 'admin@evmarketplace.com' // Update with actual email
        useCommonAlertSchema: true
      }
    ]
    // Optional: Add SMS, webhook, or Azure app push notifications
    // smsReceivers: []
    // webhookReceivers: []
  }
}

// Alert Rule: API High Response Time
resource apiResponseTimeAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}-api-high-response-time'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API response time exceeds threshold'
    severity: 2
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Response time threshold'
          metricName: 'requests/duration'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 2000 // 2 seconds
          timeAggregation: 'Average'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert Rule: API Failure Rate
resource apiFailureRateAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}-api-failure-rate'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API failure rate is high'
    severity: 1
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Failure rate threshold'
          metricName: 'requests/failed'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 10 // More than 10 failed requests
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert Rule: Function App Errors
resource functionErrorsAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = {
  name: '${alertPrefix}-function-errors'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when Azure Functions have errors'
    severity: 2
    enabled: true
    scopes: [
      appInsights.id
    ]
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.SingleResourceMultipleMetricCriteria'
      allOf: [
        {
          name: 'Function errors threshold'
          metricName: 'exceptions/count'
          metricNamespace: 'microsoft.insights/components'
          operator: 'GreaterThan'
          threshold: 5
          timeAggregation: 'Count'
          criterionType: 'StaticThresholdCriterion'
        }
      ]
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Alert Rule: Database Connection Failures
resource databaseConnectionAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: '${alertPrefix}-database-connection-failures'
  location: location
  tags: tags
  properties: {
    description: 'Alert when database connection failures are detected'
    severity: 1
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT15M'
    scopes: [
      appInsights.id
    ]
    criteria: {
      allOf: [
        {
          query: 'traces | where message contains "Failed to" and message contains "database" | summarize count() by bin(timestamp, 5m)'
          timeAggregation: 'Count'
          operator: 'GreaterThan'
          threshold: 3
          failingPeriods: {
            numberOfEvaluationPeriods: 1
            minFailingPeriodsToAlert: 1
          }
        }
      ]
    }
    actions: {
      actionGroups: [
        actionGroup.id
      ]
    }
  }
}

// Availability Test for API endpoint
resource availabilityTest 'Microsoft.Insights/webtests@2022-06-15' = if (environment == 'prod') {
  name: 'webtest-api-availability'
  location: location
  tags: union(tags, {
    'hidden-link:${appInsights.id}': 'Resource'
  })
  properties: {
    Name: 'API Availability Test'
    Description: 'Ping test for API health endpoint'
    Enabled: true
    Frequency: 300 // 5 minutes
    Timeout: 30
    Kind: 'ping'
    RetryEnabled: true
    Locations: [
      {
        Id: 'emea-nl-ams-azr' // West Europe
      }
      {
        Id: 'emea-gb-db3-azr' // UK South
      }
      {
        Id: 'us-va-ash-azr' // East US
      }
    ]
    Configuration: {
      WebTest: '<WebTest Name="API Health Check" Enabled="True" Timeout="30"><Items><Request Method="GET" Version="1.1" Url="https://app-evmarket-api-prod.azurewebsites.net/health" ThinkTime="0" Timeout="30" ParseDependentRequests="False" FollowRedirects="True" /></Items></WebTest>'
    }
    SyntheticMonitorId: 'api-availability-test'
  }
}

// Alert for availability test failures
resource availabilityAlert 'Microsoft.Insights/metricAlerts@2018-03-01' = if (environment == 'prod') {
  name: '${alertPrefix}-api-availability'
  location: 'global'
  tags: tags
  properties: {
    description: 'Alert when API availability test fails'
    severity: 0 // Critical
    enabled: true
    scopes: [
      availabilityTest.id
      appInsights.id
    ]
    evaluationFrequency: 'PT1M'
    windowSize: 'PT5M'
    criteria: {
      'odata.type': 'Microsoft.Azure.Monitor.WebtestLocationAvailabilityCriteria'
      webTestId: availabilityTest.id
      componentId: appInsights.id
      failedLocationCount: 2 // Alert if failing from 2+ locations
    }
    actions: [
      {
        actionGroupId: actionGroup.id
      }
    ]
  }
}

// Workbook for custom dashboards
resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid('workbook-evmarket-${environment}')
  location: location
  tags: tags
  kind: 'shared'
  properties: {
    displayName: 'EV Marketplace - ${environment} Dashboard'
    serializedData: '{"version":"Notebook/1.0","items":[{"type":1,"content":{"json":"# EV Marketplace Monitoring Dashboard\\n\\nOverview of API performance, user activity, and system health."}},{"type":10,"content":{"chartId":"chart-api-requests","version":"KqlParameterItem/1.0","name":"TimeRange","type":4,"value":{"durationMs":3600000},"typeSettings":{"selectableValues":[{"durationMs":300000},{"durationMs":900000},{"durationMs":1800000},{"durationMs":3600000},{"durationMs":14400000},{"durationMs":86400000}]}}}],"styleSettings":{},"fromTemplateId":"sentinel-UserWorkbook"}'
    category: 'workbook'
    sourceId: appInsights.id
  }
}

output appInsightsName string = appInsights.name
output appInsightsId string = appInsights.id
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id
output actionGroupId string = actionGroup.id
