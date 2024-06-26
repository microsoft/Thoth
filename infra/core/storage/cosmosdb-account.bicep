metadata description = 'Creates a Cosmos DB account with a SQL API database and container.'
param accountName string = 'cosmosthoth'
param databaseName string = 'ToDoList'
param defaultExperience string = 'Core (SQL)'
param kind string = 'GlobalDocumentDB'
param location string = resourceGroup().location
param defaultConsistencyLevel string = 'Session'
param maxIntervalInSeconds int = 5
param maxStalenessPrefix int = 100
param enableServerless bool = true

resource cosmos_account 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
  name: accountName
  location: location
  tags: {
    defaultExperience: defaultExperience
    'hidden-cosmos-mmspecial': ''
  }
  kind: kind
  properties: {    
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false  
    consistencyPolicy: {
      defaultConsistencyLevel: defaultConsistencyLevel
      maxIntervalInSeconds: maxIntervalInSeconds
      maxStalenessPrefix: maxStalenessPrefix
    }
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    disableLocalAuth: false
    capabilities: enableServerless ? [
      {
        name: 'EnableServerless'
      }
    ] : []
  }
  
}

resource cosmos_database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-02-15-preview' = {
  parent: cosmos_account
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

output accountName string = cosmos_account.name
