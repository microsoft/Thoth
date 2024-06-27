param name string
param location string = resourceGroup().location
param tags object = {}

@description('The name of the identity')
param identityName string

@description('The name of the Application Insights')
param applicationInsightsName string

@description('The name of the service')
param serviceName string = 'web'

@description('The storage blob endpoint')
param storageBlobEndpoint string

@description('The name of the storage container')
param storageContainerName string

@description('The search service endpoint')
param searchServiceEndpoint string

@description('The search index name')
param searchIndexName string

@description('The Form Recognizer endpoint')
param formRecognizerEndpoint string

@description('The OpenAI endpoint')
param openAiEndpoint string

@description('The OpenAI ChatGPT deployment name')
param openAiChatGptDeployment string

@description('The OpenAI Embedding deployment name')
param openAiEmbeddingDeployment string

@description('The OpenAI API key')
param openAiApiKey string

@description('Whether to use Azure OpenAI')
param useAOAI bool = true

@description('The Cosmos DB endpoint')
param cosmosEndpoint string

@description('Whether to use generated SAS keys for blob storage file preview')
param useBlobSas bool = false

param useManagedIdentity bool

// Runtime Properties
@allowed([
  'dotnet', 'dotnetcore', 'dotnet-isolated', 'node', 'python', 'java', 'powershell', 'custom'
])
param runtimeName string
param runtimeNameAndVersion string = '${runtimeName}|${runtimeVersion}'
param runtimeVersion string

// Microsoft.Web/sites Properties
param kind string = 'linux'

// Microsoft.Web/sites/config
param allowedOrigins array = ['*']
param clientAffinityEnabled bool = false
param enableOryxBuild bool = contains(kind, 'linux')
param linuxFxVersion string = runtimeNameAndVersion
param scmDoBuildDuringDeployment bool = true
param use32BitWorkerProcess bool = false

var appSettings = {
  
    APPLICATIONINSIGHTS_CONNECTION_STRING: !empty(applicationInsightsName) ? applicationInsights.properties.ConnectionString : ''
    AZURE_STORAGE_BLOB_ENDPOINT: storageBlobEndpoint
    AZURE_STORAGE_CONTAINER: storageContainerName
    AZURE_SEARCH_SERVICE_ENDPOINT: searchServiceEndpoint
    AZURE_SEARCH_INDEX: searchIndexName
    AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT: formRecognizerEndpoint
    AZURE_OPENAI_ENDPOINT: openAiEndpoint
    AZURE_OPENAI_CHATGPT_DEPLOYMENT: openAiChatGptDeployment
    AZURE_OPENAI_EMBEDDING_DEPLOYMENT: openAiEmbeddingDeployment
    COSMOS_HISTORY_ENDPOINT: cosmosEndpoint
    OPENAI_API_KEY: openAiApiKey
    USE_AOAI: useAOAI ? 'true' : 'false'
    AZURE_STORAGE_USE_BLOB_SAS: useBlobSas ? 'true' : 'false'
}

module appServicePlan '../core/host/appserviceplan.bicep' = {
  name: '${serviceName}-backend-appserviceplan'
  params: {
    name: '${serviceName}-backend-appserviceplan'
    location: location
    sku: {
      name: 'B1'
    }
  }
}

module app '../core/host/appservice.bicep' = {
  name: '${serviceName}-backend-app'
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    allowedOrigins: allowedOrigins
    applicationInsightsName: applicationInsightsName
    appServicePlanId: appServicePlan.outputs.id
    clientAffinityEnabled: clientAffinityEnabled
    enableOryxBuild: enableOryxBuild    
    kind: kind
    linuxFxVersion: linuxFxVersion
    runtimeName: runtimeName
    runtimeVersion: runtimeVersion
    runtimeNameAndVersion: runtimeNameAndVersion
    scmDoBuildDuringDeployment: scmDoBuildDuringDeployment
    use32BitWorkerProcess: use32BitWorkerProcess
    managedIdentity: useManagedIdentity
  }
}

module appSettingsConfig '../core/host/appservice-appsettings.bicep' = {
  name: '${serviceName}-backend-appsettings'
  params: {
    name: app.outputs.name
    appSettings: appSettings
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(applicationInsightsName)) {
  name: applicationInsightsName
}

output SERVICE_WEB_IDENTITY_NAME string = identityName
output SERVICE_WEB_IDENTITY_PRINCIPAL_ID string = app.outputs.identityPrincipalId
output SERVICE_WEB_NAME string = app.outputs.name
output SERVICE_WEB_URI string = app.outputs.uri
