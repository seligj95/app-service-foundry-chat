@description('Location for all resources')
param location string

@description('Tags for all resources')
param tags object

@description('Name of the App Service Plan')
param appServicePlanName string

@description('Name of the App Service')
param appServiceName string

@description('Subnet ID for VNet integration')
param appServiceSubnetId string

@description('Application Insights connection string')
param applicationInsightsConnectionString string

@description('AI Foundry endpoint')
param aiFoundryEndpoint string

@description('AI Foundry resource ID for role assignment')
param aiFoundryId string

@description('AI Foundry resource name for scoped role assignment')
param aiFoundryName string

@description('Model deployment name')
param modelDeploymentName string

// App Service Plan - P0v3
resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'P0v3'
    tier: 'PremiumV3'
  }
  kind: 'linux'
  properties: {
    reserved: true // Required for Linux
  }
}

// App Service
resource appService 'Microsoft.Web/sites@2024-04-01' = {
  name: appServiceName
  location: location
  tags: union(tags, {
    'azd-service-name': 'web'
  })
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    virtualNetworkSubnetId: appServiceSubnetId
    vnetRouteAllEnabled: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'AZURE_AI_FOUNDRY_ENDPOINT'
          value: aiFoundryEndpoint
        }
        {
          name: 'AZURE_AI_MODEL_DEPLOYMENT'
          value: modelDeploymentName
        }
      ]
    }
  }
}

// Reference existing AI Foundry resource for scoped role assignment
resource aiFoundry 'Microsoft.CognitiveServices/accounts@2025-06-01' existing = {
  name: aiFoundryName
}

// Role Assignment: App Service -> Cognitive Services OpenAI User on AI Foundry (scoped to resource)
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiFoundryId, appService.id, 'Cognitive Services OpenAI User')
  scope: aiFoundry
  properties: {
    principalId: appService.identity.principalId
    principalType: 'ServicePrincipal'
    // Cognitive Services OpenAI User role
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd')
  }
}

output appServiceId string = appService.id
output appServiceName string = appService.name
output appServiceUri string = 'https://${appService.properties.defaultHostName}'
output appServicePrincipalId string = appService.identity.principalId
