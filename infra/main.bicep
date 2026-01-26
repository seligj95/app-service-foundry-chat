targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('Name of the resource group')
param resourceGroupName string = ''

@description('Name of the AI model to deploy')
param modelName string = 'gpt-4o'

@description('Version of the AI model to deploy')
param modelVersion string = '2024-08-06'

@description('Capacity for the model deployment')
param modelCapacity int = 10

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
}

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Virtual Network
module network './modules/network.bicep' = {
  name: 'network'
  scope: rg
  params: {
    location: location
    tags: tags
    vnetName: '${abbrs.networkVirtualNetworks}${resourceToken}'
    appServiceSubnetName: 'snet-appservice'
    privateEndpointSubnetName: 'snet-privateendpoints'
  }
}

// Monitoring (Log Analytics + Application Insights)
module monitoring './modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

// AI Foundry
module aiFoundry './modules/ai-foundry.bicep' = {
  name: 'ai-foundry'
  scope: rg
  params: {
    location: location
    tags: tags
    aiFoundryName: '${abbrs.cognitiveServicesAccounts}${resourceToken}'
    projectName: 'chat-project'
    modelName: modelName
    modelVersion: modelVersion
    modelCapacity: modelCapacity
    privateEndpointSubnetId: network.outputs.privateEndpointSubnetId
    vnetId: network.outputs.vnetId
    logAnalyticsWorkspaceId: monitoring.outputs.logAnalyticsWorkspaceId
  }
}

// App Service
module appService './modules/app-service.bicep' = {
  name: 'app-service'
  scope: rg
  params: {
    location: location
    tags: tags
    appServicePlanName: '${abbrs.webServerFarms}${resourceToken}'
    appServiceName: '${abbrs.webSitesAppService}${resourceToken}'
    appServiceSubnetId: network.outputs.appServiceSubnetId
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    aiFoundryEndpoint: aiFoundry.outputs.aiFoundryEndpoint
    aiFoundryId: aiFoundry.outputs.aiFoundryId
    aiFoundryName: aiFoundry.outputs.aiFoundryName
    modelDeploymentName: modelName
  }
}

// Outputs for azd
output AZURE_LOCATION string = location
output AZURE_TENANT_ID string = tenant().tenantId
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_AI_PROJECT_ENDPOINT string = aiFoundry.outputs.aiFoundryEndpoint
output SERVICE_WEB_NAME string = appService.outputs.appServiceName
output SERVICE_WEB_URI string = appService.outputs.appServiceUri
