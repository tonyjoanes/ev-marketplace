// Azure Database for PostgreSQL Flexible Server
param location string
param environment string
param serverName string

@description('Database administrator username')
@secure()
param administratorLogin string = 'evmarketadmin'

@description('Database administrator password')
@secure()
param administratorPassword string

@description('Database name')
param databaseName string = 'evmarketplace'

// SKU tiers by environment
var skuTier = environment == 'prod' ? 'GeneralPurpose' : 'Burstable'
var skuName = environment == 'prod' ? 'Standard_D2s_v3' : 'Standard_B1ms'

// PostgreSQL Flexible Server
resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2022-12-01' = {
  name: serverName
  location: location
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    administratorLogin: administratorLogin
    administratorLoginPassword: administratorPassword
    version: '15'
    storage: {
      storageSizeGB: environment == 'prod' ? 128 : 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: environment == 'prod' ? 'Enabled' : 'Disabled'
    }
    highAvailability: {
      mode: environment == 'prod' ? 'ZoneRedundant' : 'Disabled'
    }
  }
  tags: {
    environment: environment
  }
}

// Allow Azure services
resource firewallRuleAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2022-12-01' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Create database
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2022-12-01' = {
  parent: postgresServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// Outputs
output serverName string = postgresServer.name
output databaseName string = database.name
output connectionString string = 'Host=${postgresServer.properties.fullyQualifiedDomainName};Database=${databaseName};Username=${administratorLogin};Password=${administratorPassword};SSL Mode=Require;'
