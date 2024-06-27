param apiManagementServiceName string
param apiName string = 'azure-openai-service-api'
param apiPath string = '/openai'
param managedIdentityPrincipalName string = ''

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-07-31-preview' existing = if (!empty(managedIdentityPrincipalName)){
  name: managedIdentityPrincipalName
}

resource apiManagementService 'Microsoft.ApiManagement/service@2022-08-01' existing = {
  name: apiManagementServiceName
}

var managedIdentityClientId = (!empty(managedIdentityPrincipalName)) ? userAssignedIdentity.properties.clientId : ''

resource api 'Microsoft.ApiManagement/service/apis@2023-03-01-preview' = {
  name: apiName
  parent: apiManagementService
  properties: {
    format: 'openapi'
    value: loadTextContent('inference.json')
    path: apiPath
    subscriptionRequired: false    
  }
}

var policyFile = loadTextContent('policy.xml')
var policyContent = (!empty(managedIdentityClientId)) ? replace(policyFile, '{clientId}', 'client-id="${managedIdentityClientId}"') : replace(policyFile, '{clientId}', '')

resource policy 'Microsoft.ApiManagement/service/apis/policies@2023-03-01-preview' = {
  name: 'policy'
  parent: api
  properties: {
    format: 'xml'
    value: policyContent
  }
}

