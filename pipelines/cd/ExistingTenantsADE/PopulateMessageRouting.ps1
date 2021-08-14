param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroup,
     [string] $servicePrincipalId, 
     [string] $servicePrincipalKey, 
     [string] $tenantId,
     [string] $subscriptionId     
)
 
     #remove and reinstall pkmngr and install packages
     Install-Module -Name AzTable -Force
 
     Write-Host "############## Installed AzTable successfully."
 
  try {       
     $resourceGroupName = $resourceGroup
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     $eventhubNamespace=$applicationCode + "-eventhub-" + $environmentCategory
     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $endpointType="EventHub"
     $endpointName= "event-hub-device-twin-mirror"
     $messageRoutes=@("deviceTwinMirrorDeviceConnectionState","deviceTwinMirrorLifecycle","deviceTwinMirrorTwinChange")
     $dataSources=@("DeviceConnectionStateEvents","DeviceLifecycleEvents","TwinChangeEvents")
     $eventHubs="device-twin-mirror"
     $messageEnrichmentEndpoints=@("event-hub-telemetry,event-hub-twin-change,event-hub-lifecycle,event-hub-device-twin-mirror")
 
     Write-Host "############## Got iothub records from storage."
 
     Foreach ($iotHub in $iotHubArray) {
 
          Write-Host "############## Started Updating routes in IotHub $iotHubName"
 
          $iotHubName = $iotHub.IotHubName
          $eventhubConnectionString=(Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHub $eventHubs -AuthorizationRuleName iothubroutes).PrimaryConnectionString
 
        # This is used to Create/Add an endpoint to all IoT Hub (for now it is single iothub)    
          Add-AzIotHubRoutingEndpoint `
            -ResourceGroupName $resourceGroup `
            -Name $iotHubName `
            -EndpointName $endpointName `
            -EndpointType $endpointType `
            -EndpointResourceGroup $resourceGroup `
            -EndpointSubscriptionId $subscriptionId `
            -ConnectionString $eventhubConnectionString
 
          Write-Host "############## Added Routing Endpoint IotHub $iotHubName"
 
        for ($i = 0; $i -lt $messageRoutes.Count; $i++) {
            $dataSourceEnum = [Microsoft.Azure.Commands.Management.IotHub.Models.PSRoutingSource]$dataSources[$i]
        # This is used to create a route in all IoT Hubs (for now it is single iothub)
            Add-AzIotHubRoute  `
            -ResourceGroupName $resourceGroup `
            -Name $iotHubName `
            -RouteName $messageRoutes[$i] `
            -Source $dataSourceEnum `
            -EndpointName $endpointName `
            -Enabled true
            -Condition true
         } 
 
         Write-Host "############## Added Routes IotHub $iotHubName"
 
         Set-AzIotHubMessageEnrichment `
           -ResourceGroupName $resourceGroup `
           -Name $iotHubName `
           -Key "tenant" `
           -Value $iotHub.TenantId `
           -Endpoint $messageEnrichmentEndpoints
 
         Write-Host "############## Added Message Enrichment IotHub $iotHubName"
    }
  }
  catch {
      Write-Host("An Error occured.")
      Write-Host($_)
  } 