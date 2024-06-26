// Parameters
param apimName string = 'apim-igxxmtxqiqk6i'
param location string = 'eastus2'
param adminEmail string = 'contoso@contso.com'
param organizationName string = 'contoso'
param appInsightsName string = 'appi-igxxmtxqiqk6i'

// Resources
resource apimService 'Microsoft.ApiManagement/service@2020-06-01-preview' = {
  name: apimName
  location: location
  properties: {
    publisherEmail: adminEmail
    publisherName: organizationName
  }
  sku: {
    name: 'Consumption'
    capacity: 0
  }
}

resource appInsights 'Microsoft.Insights/components@2015-05-01' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource apimLogger 'Microsoft.ApiManagement/service/loggers@2020-06-01-preview' = {
  parent: apimService
  name: '${apimName}/${appInsightsName}'
  properties: {
    loggerType: 'applicationInsights'
    description: 'Logger for Application Insights'
    credentials: {
      instrumentationKey: appInsights.properties.InstrumentationKey
    }
  }
}

resource apimDiagnostics 'Microsoft.ApiManagement/service/diagnostics@2020-06-01-preview' = {
  parent: apimService
  name: '${apimName}/applicationinsights'
  dependsOn: [
    apimLogger
  ]
  properties: {
    loggerId: apimLogger.id
  }
}
