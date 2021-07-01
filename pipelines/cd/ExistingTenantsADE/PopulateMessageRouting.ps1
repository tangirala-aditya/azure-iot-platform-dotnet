param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroup,
     [string] $servicePrincipalId, 
     [string] $servicePrincipalKey, 
     [string] $tenantId,
     [string] $subscriptionId     
)


try {       
     #remove and reinstall pkmngr and install packages
     Register-PackageSource -Name MyNuGet -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name Az.Kusto -Force
     Install-Module -Name AzTable -Force
     Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Source MyNuGet -Destination . -Force

     $resourceGroupName = $resourceGroup
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     $eventhubNamespace=$applicationCode + "eventhub" + $environmentCategory
     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $endpointType="eventhubs"
     $endpointName= "event-hub-device-twin-mirror"
     $messageRoutes=@("TwinMirrorDeviceConnectionState","TwinMirrorLifecycle","TwinMirrorTwinChange")
     $dataSources=@("DeviceConnectionStateEvents","DeviceLifecycleEvents","TwinChangeEvents")
     $eventHubs="device-twin-mirror"
     $messageEnrichmentEndpoints=@("event-hub-telemetry,event-hub-twin-change,event-hub-lifecycle,event-hub-device-twin-mirror")

     az cloud set -n AzureCloud
     az login --service-principal -u $servicePrincipalId --password $servicePrincipalKey --tenant $tenantId --allow-no-subscriptions
     az account set --subscription $subscriptionId


     Foreach ($iotHub in $iotHubArray) {
          $iotHubName = $iotHub.Name
          $enrichMessageObject[0].value=$iotHub.TenantId
          $authruleName=(Get-AzEventHubAuthorizationRule -ResourceGroupName $resourceGroup -NamespaceName $eventhubNamespace -EventHubName $eventHubs).Name
          $eventhubConnectionString=(Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -AuthorizationRuleName $authruleName).PrimaryConnectionString
     
        # This is used to Create/Add an endpoint to all IoT Hub (for now it is single iothub)    
          Add-AzIotHubRoutingEndpoint `
            -ResourceGroupName $resourceGroup `
            -Name $iotHubName `
            -EndpointName $endpointName `
            -EndpointType $endpointType `
            -EndpointResourceGroup $resourceGroup `
            -EndpointSubscriptionId $subscriptionId `
            -ConnectionString $eventhubConnectionString

        for ($i = 0; $i -lt $messageRoutes.Count; $i++) {
        # This is used to create a route in all IoT Hubs (for now it is single iothub)
        Add-AzIotHubRoute  `
           -ResourceGroupName $resourceGroup `
           -Name $iotHubName `
           -RouteName $messageRoutes[$i] `
           -Source $dataSources[$i] `
           -EndpointName $endpointName
         } 

         Set-AzIotHubMessageEnrichment `
           -ResourceGroupName $resourceGroup `
           -Name $iotHubName `
           -Key "tenant" `
           -Value $iotHub.TenantId `
           -Endpoint $messageEnrichmentEndpoints
    }
}
catch {
     Write-Host("An Error occured.")
     Write-Host($_)
}
