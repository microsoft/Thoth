targetScope = 'subscription'

@description('Name of the environment used to generate a short unique hash for resources.')
@minLength(1)
@maxLength(64)
param environmentName string

@description('Primary location for most resources')
@allowed([ 'centralus', 'eastus2', 'eastasia', 'westus', 'westeurope', 'westus2', 'australiaeast', 'eastus', 'francecentral', 'japaneast', 'nortcentralus', 'swedencentral', 'switzerlandnorth', 'uksouth' ])
param location string
param tags string = ''

@description('''
Document Intelligence location is in preview.  
Preview regions are: eastus westus2, or westeurope.  
Entering other regions while in preview will cause an error in the application.
''')
@allowed([ 'eastus', 'westus2', 'westeurope'])
param docIntPreviewLocation_See_Help string

@description('Location for the OpenAI resource group')
@allowed([ 'canadaeast', 'westus', 'eastus', 'eastus2', 'francecentral', 'swedencentral', 'switzerlandnorth', 'uksouth', 'japaneast', 'northcentralus', 'australiaeast' ])
@metadata({
  azd: {
    type: 'location'
  }
})
param openAiResourceGroupLocation string

@description('Name of the chat GPT model. Default: gpt-4o')
@allowed([ 'gpt-4o', 'gpt-4', 'gpt-35-turbo-16k', 'gpt-4-16k' ])
param azureOpenAIChatGptModelName string = 'gpt-4o'

param azureOpenAIChatGptModelVersion string ='2024-05-13'

@description('Name of the Azure Application Insights dashboard')
param applicationInsightsDashboardName string = ''

@description('Name of the Azure Application Insights resource')
param applicationInsightsName string = ''

@description('Name of the Azure App Service Plan')
param appServicePlanName string = ''

@description('Capacity of the chat GPT deployment. Default: 10')
param chatGptDeploymentCapacity int = 10

@description('Name of the chat GPT deployment')
param azureChatGptDeploymentName string = 'chat'

@description('Name of the embedding deployment. Default: embedding')
param azureEmbeddingDeploymentName string = 'embedding'

@description('Capacity of the embedding deployment. Default: 30')
param embeddingDeploymentCapacity int = 30

@description('Name of the embedding model. Default: text-embedding-3-small')
param azureEmbeddingModelName string = 'text-embedding-3-small'

@description('Location of the resource group for the Form Recognizer service')
param formRecognizerResourceGroupLocation string = docIntPreviewLocation_See_Help

@description('Name of the resource group for the Form Recognizer service')
param formRecognizerResourceGroupName string = ''

@description('Name of the Form Recognizer service')
param formRecognizerServiceName string = ''

@description('SKU name for the Form Recognizer service. Default: S0')
param formRecognizerSkuName string = 'S0'

@description('Name of the Azure Function App')
param functionServiceName string = ''

@description('Name of the Azure Log Analytics workspace')
param logAnalyticsName string = ''

@description('Name of the resource group for the OpenAI resources')
param openAiResourceGroupName string = ''

@description('Name of the OpenAI service')
param openAiServiceName string = ''

@description('SKU name for the OpenAI service. Default: S0')
param openAiSkuName string = 'S0'

@description('ID of the principal')
param principalId string = ''

@description('Type of the principal. Valid values: User,ServicePrincipal')
param principalType string = 'User'

@description('Name of the resource group')
param resourceGroupName string = ''

@description('Name of the search index. Default: gptkbindex')
param searchIndexName string = 'gptkbindex'

@description('Name of the Azure AI Search service')
param searchServiceName string = 'thothsearchdemo'

@description('Location of the resource group for the Azure AI Search service')
param searchServiceResourceGroupLocation string = location

@description('Name of the resource group for the Azure AI Search service')
param searchServiceResourceGroupName string = ''

@description('SKU name for the Azure AI Search service. Default: standard')
param searchServiceSkuName string = 'standard'

@description('Name of the storage account')
param storageAccountName string = ''

@description('Name of the storage container. Default: content')
param storageContainerName string = 'content'

@description('Location of the resource group for the storage account')
param storageResourceGroupLocation string = location

@description('Name of the resource group for the storage account')
param storageResourceGroupName string = ''

@description('Name of the web app container')
param webContainerAppName string = ''

@description('Name of the web app identity')
param webIdentityName string = ''

@description('Use Azure OpenAI service')
param useAOAI bool

@description('OpenAI API Key')
param openAIApiKey string

@description('OpenAI Embedding Model')
param openAiEmbeddingDeployment string

@description('CosmosDB Account Name')
param cosmosDbAccountName string = ''

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var baseTags = { 'azd-env-name': environmentName }
var updatedTags = union(empty(tags) ? {} : base64ToJson(tags), baseTags)


// Organize resources in a resource group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: updatedTags
}

resource azureOpenAiResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(openAiResourceGroupName) && useAOAI) {
  name: !empty(openAiResourceGroupName) ? openAiResourceGroupName : resourceGroup.name
}

resource formRecognizerResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(formRecognizerResourceGroupName)) {
  name: !empty(formRecognizerResourceGroupName) ? formRecognizerResourceGroupName : resourceGroup.name
}

resource searchServiceResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(searchServiceResourceGroupName)) {
  name: !empty(searchServiceResourceGroupName) ? searchServiceResourceGroupName : resourceGroup.name
}

resource storageResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' existing = if (!empty(storageResourceGroupName)) {
  name: !empty(storageResourceGroupName) ? storageResourceGroupName : resourceGroup.name
}

// Web frontend
module web './app/web.bicep' = {
  name: 'web'
  scope: resourceGroup
  params: {
    name: !empty(webContainerAppName) ? webContainerAppName : '${abbrs.appContainerApps}web-${resourceToken}'
    location: location
    tags: updatedTags
    runtimeName: 'dotnetcore'
    runtimeVersion: '8.0'
    identityName: !empty(webIdentityName) ? webIdentityName : '${abbrs.managedIdentityUserAssignedIdentities}web-${resourceToken}'
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    storageBlobEndpoint: storage.outputs.primaryEndpoints.blob
    storageContainerName: storageContainerName
    searchServiceEndpoint: searchService.outputs.endpoint
    searchIndexName: searchIndexName
    formRecognizerEndpoint: formRecognizer.outputs.endpoint
    openAiApiKey: useAOAI ? '' : openAIApiKey
    openAiEndpoint: useAOAI ? azureOpenAi.outputs.endpoint : ''
    openAiChatGptDeployment: useAOAI ? azureChatGptDeploymentName : ''
    openAiEmbeddingDeployment: useAOAI ? azureEmbeddingDeploymentName : ''
    useAOAI: useAOAI
    cosmosEndpoint: database.outputs.endpoint
    useManagedIdentity: true
  }
}

// Create an App Service Plan to group applications under the same payment plan and SKU
module appServicePlan './core/host/appserviceplan.bicep' = {
  name: 'appserviceplan'
  scope: resourceGroup
  params: {
    name: !empty(appServicePlanName) ? appServicePlanName : '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: updatedTags
    sku: {
      name: 'Y1'
      tier: 'Dynamic'
    }
  }
}

// The application backend
module function './app/function.bicep' = {
  name: 'function'
  scope: resourceGroup
  params: {
    name: !empty(functionServiceName) ? functionServiceName : '${abbrs.webSitesFunctions}function-${resourceToken}'
    location: location
    tags: updatedTags
    applicationInsightsName: monitoring.outputs.applicationInsightsName
    appServicePlanId: appServicePlan.outputs.id
    storageAccountName: storage.outputs.name
    allowedOrigins: [ web.outputs.SERVICE_WEB_URI ]
    useManagedIdentity: true
    appSettings: {
      AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT: formRecognizer.outputs.endpoint
      AZURE_SEARCH_SERVICE_ENDPOINT: searchService.outputs.endpoint
      AZURE_SEARCH_INDEX: searchIndexName
      AZURE_STORAGE_BLOB_ENDPOINT: storage.outputs.primaryEndpoints.blob
      AZURE_OPENAI_EMBEDDING_DEPLOYMENT: useAOAI ? azureEmbeddingDeploymentName : ''
      OPENAI_EMBEDDING_DEPLOYMENT: useAOAI ? '' : openAiEmbeddingDeployment
      AZURE_OPENAI_ENDPOINT: useAOAI ? azureOpenAi.outputs.endpoint : ''
      USE_AOAI: string(useAOAI)
      OPENAI_API_KEY: useAOAI ? '' : openAIApiKey
    }
  }
}

// Monitor application with Azure Monitor
module monitoring 'core/monitor/monitoring.bicep' = {
  name: 'monitoring'
  scope: resourceGroup
  params: {
    location: location
    tags: updatedTags
    includeApplicationInsights: true
    logAnalyticsName: !empty(logAnalyticsName) ? logAnalyticsName : '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: !empty(applicationInsightsName) ? applicationInsightsName : '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: !empty(applicationInsightsDashboardName) ? applicationInsightsDashboardName : '${abbrs.portalDashboards}${resourceToken}'
  }
}

module azureOpenAi 'core/ai/cognitiveservices.bicep' = if (useAOAI) {
  name: 'openai'
  scope: azureOpenAiResourceGroup
  params: {
    name: !empty(openAiServiceName) ? openAiServiceName : '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    location: openAiResourceGroupLocation
    tags: updatedTags
    sku: {
      name: openAiSkuName
    }
    deployments: concat([
      
      {
        name: azureEmbeddingDeploymentName
        model: {
          format: 'OpenAI'
          name: azureEmbeddingModelName
          version: '1'
        }
        sku: {
          name: 'Standard'
          capacity: embeddingDeploymentCapacity
        }
      }
    ], [
      {
        name: azureChatGptDeploymentName
        model: {
          format: 'OpenAI'
          name: azureOpenAIChatGptModelName
          version: azureOpenAIChatGptModelVersion
        }
        sku: {
          name: 'Standard'
          capacity: chatGptDeploymentCapacity
        }
      }
    ])
  }
}

module formRecognizer 'core/ai/cognitiveservices.bicep' = {
  name: 'formrecognizer'
  scope: formRecognizerResourceGroup
  params: {
    name: !empty(formRecognizerServiceName) ? formRecognizerServiceName : '${abbrs.cognitiveServicesFormRecognizer}${resourceToken}'
    kind: 'FormRecognizer'
    location: formRecognizerResourceGroupLocation
    tags: updatedTags
    sku: {
      name: formRecognizerSkuName
    }
  }
}

module searchService 'core/search/search-services.bicep' = {
  name: 'search-service'
  scope: searchServiceResourceGroup
  params: {
    name: !empty(searchServiceName) ? searchServiceName : 'gptkb-${resourceToken}'
    location: searchServiceResourceGroupLocation
    tags: updatedTags
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
    sku: {
      name: searchServiceSkuName
    }
    semanticSearch: 'free'
  }
}

module storage 'core/storage/storage-account.bicep' = {
  name: 'storage'
  scope: storageResourceGroup
  params: {
    name: !empty(storageAccountName) ? storageAccountName : '${abbrs.storageStorageAccounts}${resourceToken}'
    location: storageResourceGroupLocation
    tags: updatedTags
    publicNetworkAccess: 'Enabled'
    sku: {
      name: 'Standard_LRS'
    }
    deleteRetentionPolicy: {
      enabled: true
      days: 2
    }
    containers: [
      {
        name: storageContainerName
        publicAccess: 'Blob'
      }
    ]
  }
}

module database 'core/storage/cosmosdb-account.bicep' = {
  name: 'database'
  scope: storageResourceGroup
  params: {
    accountName: !empty(cosmosDbAccountName) ? cosmosDbAccountName : '${abbrs.documentDBDatabaseAccounts}${resourceToken}'
    location: location
    databaseName: 'chatdb'
  }
}

module history_container 'core/storage/cosmosdb-container.bicep' = {
  name: 'history_container'
  scope: storageResourceGroup
  params: {
    parentAccountName: database.outputs.accountName
    parentDatabaseName: 'chatdb'
    containerName: 'chathistory'
  }
}

module pinned_container 'core/storage/cosmosdb-container.bicep' = {
  name: 'pinned_container'
  scope: storageResourceGroup
  params: {
    parentAccountName: database.outputs.accountName
    parentDatabaseName: 'chatdb'
    containerName: 'pinnedqueries'
  }
}

// USER ROLES
module azureOpenAiRoleUser 'core/security/role.bicep' = if (useAOAI) {
  scope: azureOpenAiResourceGroup
  name: 'openai-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: principalType
  }
}

module formRecognizerRoleUser 'core/security/role.bicep' = {
  scope: formRecognizerResourceGroup
  name: 'formrecognizer-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: 'a97b65f3-24c7-4388-baec-2e87135dc908'
    principalType: principalType
  }
}

module storageRoleUser 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
    principalType: principalType
  }
}

module storageContribRoleUser 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-contribrole-user'
  params: {
    principalId: principalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: principalType
  }
}

module searchRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: principalType
  }
}

module searchContribRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-contrib-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
    principalType: principalType
  }
}

module searchSvcContribRoleUser 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-svccontrib-role-user'
  params: {
    principalId: principalId
    roleDefinitionId: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
    principalType: principalType
  }
}

// FUNCTION ROLES
module AzureOpenAiRoleFunction 'core/security/role.bicep' = if (useAOAI) {
  scope: azureOpenAiResourceGroup
  name: 'openai-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}

module formRecognizerRoleFunction 'core/security/role.bicep' = {
  scope: formRecognizerResourceGroup
  name: 'formrecognizer-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: 'a97b65f3-24c7-4388-baec-2e87135dc908'
    principalType: 'ServicePrincipal'
  }
}

module storageRoleFunction 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
    principalType: 'ServicePrincipal'
  }
}

module storageContribRoleFunction 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-contribrole-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'ServicePrincipal'
  }
}

module searchRoleFunction 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: 'ServicePrincipal'
  }
}

module searchContribRoleFunction 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-contrib-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '8ebe5a00-799e-43f5-93ac-243d3dce84a7'
    principalType: 'ServicePrincipal'
  }
}

module searchSvcContribRoleFunction 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-svccontrib-role-function'
  params: {
    principalId: function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '7ca78c08-252a-4471-8644-bb5ff32d4ba0'
    principalType: 'ServicePrincipal'
  }
}

// SYSTEM IDENTITIES
module azureOpenAiRoleBackend 'core/security/role.bicep' = if (useAOAI) {
  scope: azureOpenAiResourceGroup
  name: 'openai-role-backend'
  params: {
    principalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
    principalType: 'ServicePrincipal'
  }
}

module storageRoleBackend 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-role-backend'
  params: {
    principalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
    principalType: 'ServicePrincipal'
  }
}

module storageContribRoleBackend 'core/security/role.bicep' = {
  scope: storageResourceGroup
  name: 'storage-contribrole-backend'
  params: {
    principalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
    principalType: 'ServicePrincipal'
  }
}

module cosmosReaderRoleBackend 'core/security/cosmosdb-role.bicep' = {
  scope: storageResourceGroup
  name: 'cosmos-reader-role-backend'
  params: {
    identityPrincipalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '00000000-0000-0000-0000-000000000001'
    cosmos_account_name: database.outputs.accountName
  }
}

module cosmosContributorRoleBackend 'core/security/cosmosdb-role.bicep' = {
  scope: storageResourceGroup
  name: 'cosmos-contributor-role-backend'
  params: {
    identityPrincipalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '00000000-0000-0000-0000-000000000002'
    cosmos_account_name: database.outputs.accountName
  }
}

module searchRoleBackend 'core/security/role.bicep' = {
  scope: searchServiceResourceGroup
  name: 'search-role-backend'
  params: {
    principalId: web.outputs.SERVICE_WEB_IDENTITY_PRINCIPAL_ID
    roleDefinitionId: '1407120a-92aa-4202-b7e9-c0e197c71c8f'
    principalType: 'ServicePrincipal'
  }
}

output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
output APPLICATIONINSIGHTS_NAME string = monitoring.outputs.applicationInsightsName
output AZURE_FORMRECOGNIZER_RESOURCE_GROUP string = formRecognizerResourceGroup.name
output AZURE_FORMRECOGNIZER_SERVICE string = formRecognizer.outputs.name
output AZURE_FORMRECOGNIZER_SERVICE_ENDPOINT string = formRecognizer.outputs.endpoint
output AZURE_LOCATION string = location
output AZURE_OPENAI_RESOURCE_LOCATION string = openAiResourceGroupLocation
output AZURE_OPENAI_CHATGPT_DEPLOYMENT string = azureChatGptDeploymentName
output AZURE_OPENAI_EMBEDDING_DEPLOYMENT string = azureEmbeddingDeploymentName
output AZURE_OPENAI_ENDPOINT string = useAOAI? azureOpenAi.outputs.endpoint : ''
output AZURE_OPENAI_RESOURCE_GROUP string = useAOAI ? azureOpenAiResourceGroup.name : ''
output AZURE_OPENAI_SERVICE string = useAOAI ? azureOpenAi.outputs.name : ''
output AZURE_RESOURCE_GROUP string = resourceGroup.name
output AZURE_SEARCH_INDEX string = searchIndexName
output AZURE_SEARCH_SERVICE string = searchService.outputs.name
output AZURE_SEARCH_SERVICE_ENDPOINT string = searchService.outputs.endpoint
output AZURE_SEARCH_SERVICE_RESOURCE_GROUP string = searchServiceResourceGroup.name
output AZURE_STORAGE_ACCOUNT string = storage.outputs.name
output AZURE_STORAGE_BLOB_ENDPOINT string = storage.outputs.primaryEndpoints.blob
output AZURE_STORAGE_CONTAINER string = storageContainerName
output AZURE_STORAGE_RESOURCE_GROUP string = storageResourceGroup.name
output AZURE_TENANT_ID string = tenant().tenantId
output SERVICE_WEB_IDENTITY_NAME string = web.outputs.SERVICE_WEB_IDENTITY_NAME
output SERVICE_WEB_NAME string = web.outputs.SERVICE_WEB_NAME
output SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID string = function.outputs.SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID
output USE_AOAI bool = useAOAI
output OPENAI_EMBEDDING_DEPLOYMENT string = openAiEmbeddingDeployment
output AZURE_OPENAI_CHATGPT_MODEL_VERSION string = azureOpenAIChatGptModelVersion
output AZURE_OPENAI_CHATGPT_MODEL_NAME string = azureOpenAIChatGptModelName
output COSMOS_HISTORY_ENDPOINT string = database.outputs.endpoint
output AZURE_DOCUMENT_INTELLIGENCE_LOCATION string = docIntPreviewLocation_See_Help
