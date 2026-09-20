// Subscription-scoped entry point (what azd expects). Creates one resource group and
// delegates everything else to resources.bicep.
targetScope = 'subscription'

@minLength(1)
@maxLength(40)
@description('Name of the azd environment; used to derive resource names.')
param environmentName string

@description('Azure region for all resources.')
param location string

@description('App Service plan SKU. B1 for the demo (WebSockets + Always On); F1 is free but limited.')
@allowed(['F1', 'B1', 'B2', 'S1'])
param appServiceSku string = 'B1'

@description('Entra tenant id used to validate API tokens.')
param entraTenantId string

@description('Client (application) id of the API app registration; used as the JWT audience.')
param apiClientId string

@description('Azure DevOps organization name (the part after dev.azure.com/).')
param azdoOrg string

@description('Azure DevOps project name.')
param azdoProject string

@secure()
@description('Azure DevOps PAT with Work Items read/write. Used by the Logic App to file bugs.')
param azdoPat string

@description('GitHub repository as owner/name. Logic App B dispatches the agent workflow here.')
param githubRepo string

@secure()
@description('GitHub token (fine-grained PAT with contents:write) able to call repository_dispatch.')
param githubDispatchToken string

var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${environmentName}'
  location: location
  tags: tags
}

module resources 'resources.bicep' = {
  name: 'resources'
  scope: rg
  params: {
    environmentName: environmentName
    location: location
    tags: tags
    appServiceSku: appServiceSku
    entraTenantId: entraTenantId
    apiClientId: apiClientId
    azdoOrg: azdoOrg
    azdoProject: azdoProject
    azdoPat: azdoPat
    githubRepo: githubRepo
    githubDispatchToken: githubDispatchToken
  }
}

output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_WEBAPP_NAME string = resources.outputs.webAppName
output AZURE_WEBAPP_URL string = resources.outputs.webAppUrl
output APPINSIGHTS_RESOURCE_ID string = resources.outputs.appInsightsId
output APPINSIGHTS_CONNECTION_STRING string = resources.outputs.appInsightsConnectionString
output LOG_ANALYTICS_WORKSPACE_ID string = resources.outputs.workspaceCustomerId
output LOGIC_APP_B_NAME string = resources.outputs.bugCreatedLogicAppName
