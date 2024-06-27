param cosmos_account_name string
param identityPrincipalId string
param roleDefinitionId string

resource cosmos_account 'Microsoft.DocumentDB/databaseAccounts@2024-05-15' existing = {
  name: cosmos_account_name
}

resource sqlRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(roleDefinitionId, identityPrincipalId, cosmos_account.id)
  parent: cosmos_account
  properties:{
    principalId: identityPrincipalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmos_account.name}/sqlRoleDefinitions/${roleDefinitionId}'
    scope: cosmos_account.id
  }
}
