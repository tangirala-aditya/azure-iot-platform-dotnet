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

     $resourceGroupName = $resourceGroup
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     $eventhubNamespace=$applicationCode + "eventhub" + $environmentCategory
     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $endpointType="eventhubs"
     $endpointName= "event-hub-device-twin-mirror"
     $messageRoutes=@("deviceTwinMirrorDeviceConnectionState","deviceTwinMirrorLifecycle","deviceTwinMirrorTwinChange")
     $dataSources=@("DeviceConnectionStateEvents","DeviceLifecycleEvents","TwinChangeEvents")
     $eventHubs="device-twin-mirror"
     $messageEnrichmentEndpoints=@("event-hub-telemetry,event-hub-twin-change,event-hub-lifecycle,event-hub-device-twin-mirror")

     az cloud set -n AzureCloud
     az login --service-principal -u $servicePrincipalId --password $servicePrincipalKey --tenant $tenantId --allow-no-subscriptions
     az account set --subscription $subscriptionId

     Write-Host "############## Got iothub records from storage."

     Foreach ($iotHub in $iotHubArray) {

          Write-Host "############## Started Updating routes in IotHub $iotHubName"

          $iotHubName = $iotHub.Name
          # $enrichMessageObject[0].value=$iotHub.TenantId
          $authruleName=(Get-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroup -NamespaceName $eventhubNamespace -EventHubName $eventHubs).Name
          $eventhubConnectionString=(Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $eventHubs -AuthorizationRuleName $authruleName).PrimaryConnectionString
     
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
        # This is used to create a route in all IoT Hubs (for now it is single iothub)
        Add-AzIotHubRoute  `
           -ResourceGroupName $resourceGroup `
           -Name $iotHubName `
           -RouteName $messageRoutes[$i] `
           -Source $dataSources[$i] `
           -EndpointName $endpointName
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
