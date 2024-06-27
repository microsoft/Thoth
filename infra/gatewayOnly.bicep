// Main bicep template
targetScope = 'resourceGroup'

@description('Name of existing user assigned identity. Will default to SystemAssigned if empty.')
param managedIdentityPrincipalName string = ''
param location string = resourceGroup().location
param prefix string = 'hygeia${uniqueString(resourceGroup().id)}'
param aoaiPrimaryAccount string = 'km-openai-e8a8fe'
param aoaiSecondaryAccount string = 'km-openai-e8a8fe'
param applicationInsightsName string = '${prefix}-ai'
param tags object = {}
param apimName string = '${prefix}-apim'
param sku string = 'Consumption'

module apim 'modules/gateway/apim.bicep' = {
  name: 'apim'
  params: {
    tags: tags
    applicationInsightsName: applicationInsightsName
    name: apimName
    location: location
    sku: sku
    aoaiPrimaryAccount: aoaiPrimaryAccount
    aoaiSecondaryAccount: aoaiSecondaryAccount
  }
}

module aoaiApi 'modules/gateway/openai-apim-api.bicep' = {
  name: 'aoaiApi'
  params: {
    apiManagementServiceName: apim.outputs.apimServiceName
    managedIdentityPrincipalName: managedIdentityPrincipalName
  }
}
