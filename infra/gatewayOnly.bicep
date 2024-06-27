// Main bicep template
targetScope = 'resourceGroup'

@description('Name of existing user assigned identity. Will default to SystemAssigned if empty.')
param managedIdentityPrincipalName string = ''
param location string = resourceGroup().location
param prefix string = 'thoth${uniqueString(resourceGroup().id)}'
param aoaiPrimaryAccount string = ''
param aoaiSecondaryAccount string = ''
param applicationInsightsName string = '${prefix}-ai'
param tags object = {}
param apimName string = '${prefix}-apim'
param sku string = 'Consumption'
param skuCount int = 0

module apim 'core/gateway/apim.bicep' = {
  name: 'apim'
  params: {
    tags: tags
    applicationInsightsName: applicationInsightsName
    name: apimName
    location: location
    sku: sku
    aoaiPrimaryAccount: aoaiPrimaryAccount
    aoaiSecondaryAccount: aoaiSecondaryAccount
    skuCount: skuCount
  }
}

module aoaiApi 'core/gateway/openai-apim-api.bicep' = {
  name: 'aoaiApi'
  params: {
    apiManagementServiceName: apim.outputs.apimServiceName
    managedIdentityPrincipalName: managedIdentityPrincipalName
  }
}
