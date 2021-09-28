param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroupName,
     [string] $servicePrincipalId, 
     [string] $servicePrincipalKey, 
     [string] $tenantId,
     [string] $subscriptionId,
     [string] $storageAccountName,
     [string] $eventhubNamespace     
)
      
     #remove and reinstall pkmngr and install packages
     Install-Module -Name AzTable -Force

     Write-Host "############## Installed AzTable successfully."

     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $endpointType = "EventHub"
     $endpointName = "event-hub-device-twin-mirror"
     $messageRoutes = @("deviceTwinMirrorDeviceConnectionState","deviceTwinMirrorLifecycle","deviceTwinMirrorTwinChange")
     $dataSources = @("deviceconnectionstateevents","devicelifecycleevents","twinchangeevents")
     $eventHubs = "device-twin-mirror"
     #$messageEnrichmentEndpoints = @("event-hub-telemetry,event-hub-twin-change,event-hub-lifecycle,event-hub-device-twin-mirror")

     az cloud set -n AzureCloud
     az login --service-principal -u $servicePrincipalId --password $servicePrincipalKey --tenant $tenantId --allow-no-subscriptions
     az account set --subscription $subscriptionId

     Write-Host "############## Got iothub records from storage."

     Foreach ($iotHub in $iotHubArray) {

          Write-Host "############## Started Updating routes in IotHub $iotHubName"

          $iotHubName = $iotHub.IotHubName
          $eventhubConnectionString=(Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHub $eventHubs -AuthorizationRuleName "iothubroutes").PrimaryConnectionString
     
        # This is used to Create/Add an endpoint to all IoT Hub (for now it is single iothub)    
          Add-AzIotHubRoutingEndpoint `
            -ResourceGroupName $resourceGroupName `
            -Name $iotHubName `
            -EndpointName $endpointName `
            -EndpointType $endpointType `
            -EndpointResourceGroup $resourceGroupName `
            -EndpointSubscriptionId $subscriptionId `
            -ConnectionString $eventhubConnectionString

          Write-Host "############## Added Routing Endpoint IotHub $iotHubName"

        for ($i = 0; $i -lt $messageRoutes.Count; $i++) {
          # This is used to create a route in all IoT Hubs (for now it is single iothub)
          az iot hub route create --hub-name $iotHubName --endpoint-name $endpointName --source $dataSources[$i] --enabled true --condition true -n $messageRoutes[$i] --resource-group $resourceGroupName
         } 

         Write-Host "############## Added Routes IotHub $iotHubName"

         az iot hub message-enrichment update --key "tenant" --value $iotHub.TenantId  --endpoints "event-hub-telemetry" "event-hub-twin-change" "event-hub-lifecycle" "event-hub-device-twin-mirror" -n $iotHubName         

         Write-Host "############## Added Message Enrichment IotHub $iotHubName"
    }