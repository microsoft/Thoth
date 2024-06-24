metadata description = 'Creates a Cosmos DB account with a SQL API database and container.'
param accountName string = 'cosmosthoth'
param databaseName string = 'ToDoList'
param defaultExperience string = 'Core (SQL)'
param kind string = 'GlobalDocumentDB'
@allowed([ 'Enabled', 'Disabled' ])
param publicNetworkAccess string = 'Disabled'
param location string = resourceGroup().location
param minimalTlsVersion string = 'Tls12'
param defaultConsistencyLevel string = 'Session'
param maxIntervalInSeconds int = 5
param maxStalenessPrefix int = 100
param enableServerless bool = true
param schemaType string = 'WellDefined'

resource databaseAccounts_cosmos_thoth_name_resource 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
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

resource databaseAccounts_cosmos_thoth_name_Database001 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2024-02-15-preview' = {
  parent: databaseAccounts_cosmos_thoth_name_resource
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource databaseAccounts_cosmos_thoth_name_00000000_0000_0000_0000_000000000001 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-02-15-preview' = {
  parent: databaseAccounts_cosmos_thoth_name_resource
  name: '00000000-0000-0000-0000-000000000001'
  properties: {
    roleName: 'Cosmos DB Built-in Data Reader'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_cosmos_thoth_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/executeQuery'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/readChangeFeed'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/read'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_cosmos_thoth_name_00000000_0000_0000_0000_000000000002 'Microsoft.DocumentDB/databaseAccounts/sqlRoleDefinitions@2024-02-15-preview' = {
  parent: databaseAccounts_cosmos_thoth_name_resource
  name: '00000000-0000-0000-0000-000000000002'
  properties: {
    roleName: 'Cosmos DB Built-in Data Contributor'
    type: 'BuiltInRole'
    assignableScopes: [
      databaseAccounts_cosmos_thoth_name_resource.id
    ]
    permissions: [
      {
        dataActions: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
        ]
        notDataActions: []
      }
    ]
  }
}

resource databaseAccounts_cosmos_thoth_name_ToDoList_Items 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-02-15-preview' = {
  parent: databaseAccounts_cosmos_thoth_name_Database001
  name: 'Items'
  properties: {
    resource: {
      id: 'Items'
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/"_etag"/?'
          }
        ]
      }
      partitionKey: {
        paths: [
          '/partitionKey'
        ]
        kind: 'Hash'
      }
      uniqueKeyPolicy: {
        uniqueKeys: []
      }
      conflictResolutionPolicy: {
        mode: 'LastWriterWins'
        conflictResolutionPath: '/_ts'
      }
      computedProperties: []
    }
  }
}
