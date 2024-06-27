param name string
param location string = resourceGroup().location
param tags object = {}

param allowedOrigins array = []
param applicationInsightsName string = ''
param appServicePlanId string
@secure()
param appSettings object = {}
param serviceName string = 'function'
param storageAccountName string
param useManagedIdentity bool

module function '../core/host/functions.bicep' = {
  name: '${serviceName}-function'
  params: {
    name: name
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    allowedOrigins: allowedOrigins
    alwaysOn: false
    appSettings: appSettings
    applicationInsightsName: applicationInsightsName
    appServicePlanId: appServicePlanId
    runtimeName: 'dotnet-isolated'
    runtimeVersion: '8.0'
    storageAccountName: storageAccountName
    scmDoBuildDuringDeployment: false
    managedIdentity: useManagedIdentity
  }
}

output SERVICE_FUNCTION_IDENTITY_PRINCIPAL_ID string = function.outputs.identityPrincipalId
output SERVICE_FUNCTION_NAME string = function.outputs.name
output SERVICE_FUNCTION_URI string = function.outputs.uri
