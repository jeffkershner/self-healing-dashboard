// Everything inside the resource group: hosting, monitoring, alerting, and the two Logic Apps
// that turn an exception into an Azure DevOps bug and a bug into a GitHub agent run.
param environmentName string
param location string
param tags object
param appServiceSku string
param entraTenantId string
param apiClientId string
param azdoOrg string
param azdoProject string
@secure()
param azdoPat string
param githubRepo string
@secure()
param githubDispatchToken string

var suffix = toLower(uniqueString(subscription().id, resourceGroup().id, environmentName))
var webAppName = 'app-${environmentName}-${suffix}'

// ---------- Monitoring ----------

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${environmentName}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: 1 }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${environmentName}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    IngestionMode: 'LogAnalytics'
    RetentionInDays: 30
  }
}

// ---------- Hosting ----------

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'plan-${environmentName}'
  location: location
  tags: tags
  kind: 'linux'
  sku: { name: appServiceSku }
  properties: { reserved: true }
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: webAppName
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      // The publish output carries both the client and server runtimeconfig files, so the image
      // cannot infer the entry point on its own.
      appCommandLine: 'dotnet Dashboard.Server.dll'
      webSocketsEnabled: true
      alwaysOn: appServiceSku != 'F1'
      http20Enabled: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsights.properties.ConnectionString }
        { name: 'Auth__Mode', value: 'AzureAd' }
        { name: 'AzureAd__Instance', value: environment().authentication.loginEndpoint }
        { name: 'AzureAd__TenantId', value: entraTenantId }
        { name: 'AzureAd__ClientId', value: apiClientId }
        { name: 'SCM_DO_BUILD_DURING_DEPLOYMENT', value: 'false' }
        // Mount each deployment zip read-only and swap atomically; copying files over a running
        // process throws InvalidProgramException during the switch, which would file bogus bugs.
        { name: 'WEBSITE_RUN_FROM_PACKAGE', value: '1' }
      ]
    }
  }
}

// ---------- Logic App A: alert -> Azure DevOps bug ----------

resource alertToBug 'Microsoft.Logic/workflows@2019-05-01' = {
  name: 'logic-${environmentName}-alert-to-bug'
  location: location
  tags: tags
  identity: { type: 'SystemAssigned' }
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('logicapps/alert-to-bug.json')
    parameters: {
      workspaceId: { value: workspace.properties.customerId }
      appInsightsResourceId: { value: appInsights.id }
      azdoOrg: { value: azdoOrg }
      azdoProject: { value: azdoProject }
      azdoPat: { value: azdoPat }
    }
  }
}

// Lets Logic App A run KQL against the workspace with its managed identity.
resource logReader 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(workspace.id, alertToBug.id, 'LogAnalyticsReader')
  scope: workspace
  properties: {
    principalId: alertToBug.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '73c42c96-874c-492b-b04d-ab87d138a893')
  }
}

// ---------- Logic App B: Azure DevOps bug created -> GitHub repository_dispatch ----------

resource bugCreated 'Microsoft.Logic/workflows@2019-05-01' = {
  name: 'logic-${environmentName}-bug-created'
  location: location
  tags: tags
  properties: {
    state: 'Enabled'
    definition: loadJsonContent('logicapps/bug-created.json')
    parameters: {
      githubRepo: { value: githubRepo }
      githubToken: { value: githubDispatchToken }
    }
  }
}

// ---------- Alerting ----------

resource actionGroup 'Microsoft.Insights/actionGroups@2023-01-01' = {
  name: 'ag-${environmentName}-exceptions'
  location: 'global'
  tags: tags
  properties: {
    groupShortName: 'exceptions'
    enabled: true
    logicAppReceivers: [
      {
        name: 'alert-to-bug'
        resourceId: alertToBug.id
        callbackUrl: listCallbackUrl(resourceId('Microsoft.Logic/workflows/triggers', alertToBug.name, 'manual'), '2019-05-01').value
        useCommonAlertSchema: true
      }
    ]
  }
}

resource exceptionAlert 'Microsoft.Insights/scheduledQueryRules@2023-03-15-preview' = {
  name: 'alert-${environmentName}-exceptions'
  location: location
  tags: tags
  properties: {
    displayName: 'Server exceptions detected'
    description: 'Fires when the dashboard API records any server-side exception. Split by problemId so each distinct failure is its own alert.'
    severity: 2
    enabled: true
    evaluationFrequency: 'PT5M'
    windowSize: 'PT5M'
    scopes: [appInsights.id]
    targetResourceTypes: ['microsoft.insights/components']
    autoMitigate: false
    criteria: {
      allOf: [
        {
          query: 'exceptions | where client_Type != "Browser" | summarize Count = sum(itemCount) by problemId, type, outerMessage'
          timeAggregation: 'Total'
          metricMeasureColumn: 'Count'
          dimensions: [
            { name: 'problemId', operator: 'Include', values: ['*'] }
            { name: 'type', operator: 'Include', values: ['*'] }
            { name: 'outerMessage', operator: 'Include', values: ['*'] }
          ]
          operator: 'GreaterThan'
          threshold: 0
          failingPeriods: { numberOfEvaluationPeriods: 1, minFailingPeriodsToAlert: 1 }
        }
      ]
    }
    actions: { actionGroups: [actionGroup.id] }
  }
}

output webAppName string = web.name
output webAppUrl string = 'https://${web.properties.defaultHostName}'
output appInsightsId string = appInsights.id
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output workspaceCustomerId string = workspace.properties.customerId
output bugCreatedLogicAppName string = bugCreated.name
