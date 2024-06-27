
param name string
param location string = resourceGroup().location
param aoaiPrimaryAccount string
param aoaiSecondaryAccount string
param managedIdentityPrincipalName string = ''
param tags object = {}

@description('The email address of the owner of the service')
@minLength(1)
param publisherEmail string = 'noreply@microsoft.com'

@description('The name of the owner of the service')
@minLength(1)
param publisherName string = 'n/a'

@description('The pricing tier of this API Management service')
@allowed([
  'Consumption'
  'Developer'
  'Standard'
  'Premium'
])
param sku string = 'Consumption'

@description('The instance size of this API Management service.')
@allowed([ 0, 1, 2 ])
param skuCount int = 0

@description('Azure Application Insights Name')
param applicationInsightsName string

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = if (!empty(managedIdentityPrincipalName)) {
  name: managedIdentityPrincipalName
}

var apimIdentity = (empty(managedIdentityPrincipalName)) ? {
  type: 'SystemAssigned'
} : {
  type: 'UserAssigned'
  userAssignedIdentities: {
    '${userAssignedIdentity.id}': {}
  }
}

resource apimService 'Microsoft.ApiManagement/service@2021-08-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': name })
  sku: {
    name: sku
    capacity: (sku == 'Consumption') ? 0 : ((sku == 'Developer') ? 1 : skuCount)
  }
  identity: apimIdentity
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    // Custom properties are not supported for Consumption SKU
    customProperties: sku == 'Consumption' ? {} : {
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_RSA_WITH_AES_128_GCM_SHA256': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_RSA_WITH_AES_256_CBC_SHA256': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_RSA_WITH_AES_128_CBC_SHA256': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_RSA_WITH_AES_256_CBC_SHA': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TLS_RSA_WITH_AES_128_CBC_SHA': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Ciphers.TripleDes168': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls10': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Tls11': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Protocols.Ssl30': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls10': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Tls11': 'false'
      'Microsoft.WindowsAzure.ApiManagement.Gateway.Security.Backend.Protocols.Ssl30': 'false'
    }
  }
}

// Create default subscription
resource apimDefaultDeveloperSubscription 'Microsoft.ApiManagement/service/subscriptions@2023-05-01-preview' = {
  parent: apimService
  name: 'default-developer'
  properties: {
    scope: '/apis'
    displayName: 'default-developer'
    state: 'active'
    allowTracing: false
  }
}


resource apimLogger 'Microsoft.ApiManagement/service/loggers@2021-12-01-preview' = if (!empty(applicationInsightsName)) {
  name: 'app-insights-logger'
  parent: apimService
  properties: {
    credentials: {
      instrumentationKey: applicationInsights.properties.InstrumentationKey
    }
    description: 'Logger to Azure Application Insights'
    isBuffered: false
    loggerType: 'applicationInsights'
    resourceId: applicationInsights.id
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = if (!empty(applicationInsightsName)) {
  name: applicationInsightsName
}

resource primarybackend 'Microsoft.ApiManagement/service/backends@2023-03-01-preview' = {
  name: 'primary'
  parent: apimService
  properties: {
    description: 'Primary LLM deployment endpoint'
    protocol: 'http'
    url: 'https://${aoaiPrimaryAccount}.openai.azure.com/openai'
  }
}

resource secondarybackend 'Microsoft.ApiManagement/service/backends@2023-03-01-preview' = {
  name: 'secondary'
  parent: apimService
  properties: {
    description: 'Secondary LLM deployment endpoint'
    protocol: 'http'
    url: 'https://${aoaiSecondaryAccount}.openai.azure.com/openai'
  }
}

output apimServiceName string = apimService.name
output apimServiceUrl string = apimService.properties.gatewayUrl
