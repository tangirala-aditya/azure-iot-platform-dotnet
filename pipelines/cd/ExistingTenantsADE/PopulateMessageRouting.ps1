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
  try {

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

          $iotHubName = $iotHub.IotHubName
          $eventhubConnectionString=(Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHub $eventHubs -AuthorizationRuleName "iothubroutes").PrimaryConnectionString

          Write-Host "############## Started Updating routes in IotHub $iotHubName"
          
          $isRoutingEndPointExists=(Get-AzIotHubRoutingEndpoint -ResourceGroupName $resourceGroupName -Name $iotHubName | Where-Object {$_.Name -eq $endpointName}).Count

          if($isRoutingEndPointExists -eq 0) {
          # This is used to Create/Add an endpoint to all IoT Hub   
          Add-AzIotHubRoutingEndpoint `
          -ResourceGroupName $resourceGroupName `
          -Name $iotHubName `
          -EndpointName $endpointName `
          -EndpointType $endpointType `
          -EndpointResourceGroup $resourceGroupName `
          -EndpointSubscriptionId $subscriptionId `
          -ConnectionString $eventhubConnectionString

          Write-Host "############## Added Routing Endpoint IotHub $iotHubName"
          }
          else {
            Write-Host "############## Routing Endpoint Already Exists for Iothub $iotHubName "
          }
  
        for ($i = 0; $i -lt $messageRoutes.Count; $i++) {

          $isMessageRouteExists=$(az iot hub route list -g $resourceGroupName --hub-name $iotHubName --query "[?name=='$dataSources[$i]'] | length(@)")
          
          if($isMessageRouteExists -ne 0) {
            # This is used to create a route in all IoT Hubs
            az iot hub route create --hub-name $iotHubName --endpoint-name $endpointName --source $dataSources[$i] --enabled true --condition true -n $messageRoutes[$i] --resource-group $resourceGroupName
            Write-Host "############## Added Routes IotHub $iotHubName"           
          }
          else {
            $messageRoute = $messageRoutes[$i]
            $dataSource = $dataSources[$i]
            Write-Host "############## Message Route $messageRoute Already Exists with Source $dataSource for IotHub $iotHubName"            
          }          
         
         } 

         az iot hub message-enrichment update --key "tenant" --value $iotHub.TenantId  --endpoints "event-hub-telemetry" "event-hub-twin-change" "event-hub-lifecycle" "event-hub-device-twin-mirror" -n $iotHubName         

         Write-Host "############## Added Message Enrichment IotHub $iotHubName"
    }
  }
  catch {
      Write-Host("An Error occured.")
      Write-Host($_)
  }    