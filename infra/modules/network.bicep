@description('Location for all resources')
param location string

@description('Tags for all resources')
param tags object

@description('Name of the virtual network')
param vnetName string

@description('Name of the App Service integration subnet')
param appServiceSubnetName string

@description('Name of the private endpoints subnet')
param privateEndpointSubnetName string

@description('Address prefix for the virtual network')
param vnetAddressPrefix string = '10.0.0.0/16'

@description('Address prefix for the App Service subnet')
param appServiceSubnetAddressPrefix string = '10.0.1.0/26'

@description('Address prefix for the private endpoints subnet')
param privateEndpointSubnetAddressPrefix string = '10.0.2.0/27'

resource vnet 'Microsoft.Network/virtualNetworks@2024-05-01' = {
  name: vnetName
  location: location
  tags: tags
  properties: {
    addressSpace: {
      addressPrefixes: [
        vnetAddressPrefix
      ]
    }
    subnets: [
      {
        name: appServiceSubnetName
        properties: {
          addressPrefix: appServiceSubnetAddressPrefix
          delegations: [
            {
              name: 'Microsoft.Web.serverFarms'
              properties: {
                serviceName: 'Microsoft.Web/serverFarms'
              }
            }
          ]
        }
      }
      {
        name: privateEndpointSubnetName
        properties: {
          addressPrefix: privateEndpointSubnetAddressPrefix
          privateEndpointNetworkPolicies: 'Disabled'
        }
      }
    ]
  }
}

output vnetId string = vnet.id
output vnetName string = vnet.name
output appServiceSubnetId string = vnet.properties.subnets[0].id
output privateEndpointSubnetId string = vnet.properties.subnets[1].id
