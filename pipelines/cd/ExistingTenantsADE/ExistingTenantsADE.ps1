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
     $resourceGroupName = $resourceGroup
     $clusterName = $applicationCode + "kusto" + $environmentCategory
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     $appConfigurationName=$applicationCode + "-appconfig-" + $environmentCategory
     
     #remove and reisntall pkmngr and install packages
     Register-PackageSource -Name MyNuGet -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name Az.Kusto -Force
     Install-Module -Name AzTable -Force
     Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Source MyNuGet -Destination . -Force

     Write-Host "############## Installed Kusto, AzTable and Kusto.Tools successfully."


     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $location = (Get-AzResourceGroup -Name $resourceGroupName | Select-Object location).location 
     az cloud set -n AzureCloud
     az login --service-principal -u $servicePrincipalId --password $servicePrincipalKey --tenant $tenantId --allow-no-subscriptions
     az account set --subscription $subscriptionId


     Foreach ($iotHub in $iotHubArray) {
          $iotTenantId=$iotHub.TenantId
          $eventhubNamespace="eventhub-" + $iotTenantId.SubString(0,8)
          $telemetryEventhubName="$iotTenantId-telemetry"
          $deviceTwinEventhubName="$iotTenantId-devicetwin"
          if((Test-AzEventHubName -Namespace $eventhubNamespace).NameAvailable){
               New-AzEventHubNamespace -ResourceGroupName $resourceGroupName -Name $eventhubNamespace -Location $location                   
          }
          else {
               Write-Host "############## EventHub NameSpace Already $eventhubNamespace." 
          }
          #Place the EventHub Namespace primary connectionstting => appConfiguration
          $connectionString=Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -AuthorizationRuleName RootManageSharedAccessKey
          az appconfig kv set --name $appConfigurationName --key "tenant:$iotTenantId​:eventHubConn" --value $connectionString.PrimaryConnectionString  --yes
          $isEventHubExists=Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $telemetryEventhubName -ErrorAction Ignore
          if($isEventHubExists  -eq $null){
               Write-Host "############## Creating EventHub $telemetryEventhubName" 
               New-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $telemetryEventhubName -MessageRetentionInDays 1
          }
          else { 
               Write-Host "############## EventHub Already exists $telemetryEventhubName." 
          }

          $isdeviceTwinEventHubExists=Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $deviceTwinEventhubName -ErrorAction Ignore
          if($isdeviceTwinEventHubExists  -eq $null){
               Write-Host "############## Creating EventHub $deviceTwinEventhubName" 
               New-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $deviceTwinEventhubName -MessageRetentionInDays 1
          }
          else { 
               Write-Host "############## EventHub Already exists $deviceTwinEventhubName." 
          }
		$eventHubResourceId = (Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $telemetryEventhubName).Id
          $IotHubName = $iotHub.IotHubName
          Write-host("############## Creating required for $IotHubName.")
          #create and check db 
          $databaseName = "Telemetry-" + $iotHub.TenantId
          $mappingName = "'TelemetryEvents_JSON_Mapping-" + $iotHub.TenantId + "'"
          $dataConMappingName = $mappingName.Split("'")[1]
          $dataconnectionName = "TelemetryDataConnect-" + $iotHub.TenantId.Split("-")[0]
          $IotHubResourceId = (Get-AzIotHub -ResourceGroupName $resourceGroupName -Name $iotHub.IotHubName).Id
          $clusterURI = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Uri          
          $clusterLocation = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
          ##checking if Name exists.
          if ((Test-AzKustoDatabaseNameAvailability -ResourceGroupName $ResourceGroupName -ClusterName $clusterName -Name $databaseName -Type Microsoft.Kusto/Clusters/Databases).NameAvailable) {
               New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $databaseName -SoftDeletePeriod 30:00:00:00 -HotCachePeriod 0:00:00:00 -Kind ReadWrite -Location $clusterLocation
               Write-Host "############## Created DataBase $databaseName."
               Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $databaseName  

               #change the names in the script file for mapping Name
               (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace 'MAPPINGNAME', $mappingName | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop
               Write-Host "############## Changed the path in the script file!"

               $connStr = "Data Source=" + $clusterURI + ";Initial Catalog=" + $databaseName + ";Application Client Id=" + $servicePrincipalId + ";Application Key=" + $servicePrincipalKey + ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id=" + $tenantId
               Write-Host $connStr
               Microsoft.Azure.Kusto.Tools.5.1.0\Tools\Kusto.Cli.exe $connStr -script:".\pipelines\cd\ExistingTenantsADE\script.txt"
               Write-Host "############## Executed the Kusto Script."

               #REVERT change the names in the script file for mapping Name
               (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace $mappingName, 'MAPPINGNAME' | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop
               Write-Host "############## Reverted the change in the script file."
          }
          else {
               write-host("############## There is already a Database with the Name $databaseName.")
          }

          #Printing the variables
          Write-Host "dataConMappingName: $dataConMappingName"
          Write-Host "clusterLocation: $clusterLocation"
          Write-Host "IotHubResourceId: $IotHubResourceId"
          Write-Host "dataconnectionName: $dataconnectionName"
          Write-Host "databaseName: $databaseName"
          Write-Host "clusterName: $clusterName"
          Write-Host "resourceGroupName: $resourceGroupName"

          #Data Connection
          ##checking if Name exists.
          if ((Test-AzKustoDataConnectionNameAvailability -ClusterName $clusterName -DatabaseName $databaseName -ResourceGroupName $resourceGroupName -Name $dataconnectionName -Type Microsoft.Kusto/Clusters/Databases/dataConnections).NameAvailable) {
               New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Location $clusterLocation -Kind "EventHub" -EventHubResourceId $eventHubResourceId -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $dataConMappingName
               Write-Host "############## Created Data Connection."
          }
          else {
               write-host("############## There is already a Data conection with the Name $dataconnectionName.")
          }

        $deviceTwinEventHubResourceId = (Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $deviceTwinEventhubName).Id
          $IotHubName = $iotHub.IotHubName
          Write-host("############## Creating required for $IotHubName.")
          #create and check db 
          $iotDatabaseName = "IoT-" + $iotHub.TenantId
          $deviceTwinMappingName = "'DeviceTwinEvents_JSON_Mapping-" + $iotHub.TenantId + "'"
          $deviceTwinDataConMappingName = $deviceTwinMappingName.Split("'")[1]
          $deviceTwinDataconnectionName = "DeviceTwinDataConnect-" + $iotHub.TenantId.Split("-")[0]
          $clusterURI = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Uri          
          $clusterLocation = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
          ##checking if Name exists.
          if ((Test-AzKustoDatabaseNameAvailability -ResourceGroupName $ResourceGroupName -ClusterName $clusterName -Name $iotDatabaseName -Type Microsoft.Kusto/Clusters/Databases).NameAvailable) {
               New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $iotDatabaseName -HotCachePeriod 0:00:00:00 -Kind ReadWrite -Location $clusterLocation
               Write-Host "############## Created DataBase $iotDatabaseName."
               Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $iotDatabaseName  

               #change the names in the script file for mapping Name
               (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script2.txt -Raw) -replace 'MAPPINGNAME', $deviceTwinMappingName | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script2.txt -ErrorAction Stop
               Write-Host "############## Changed the path in the script file!"

               $connStr = "Data Source=" + $clusterURI + ";Initial Catalog=" + $iotDatabaseName + ";Application Client Id=" + $servicePrincipalId + ";Application Key=" + $servicePrincipalKey + ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id=" + $tenantId
               Write-Host $connStr
               Microsoft.Azure.Kusto.Tools.5.1.0\Tools\Kusto.Cli.exe $connStr -script:".\pipelines\cd\ExistingTenantsADE\script2.txt"
               Write-Host "############## Executed the Kusto Script."

               #REVERT change the names in the script file for mapping Name
               (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script2.txt -Raw) -replace $deviceTwinMappingName, 'MAPPINGNAME' | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script2.txt -ErrorAction Stop
               Write-Host "############## Reverted the change in the script file."
          }
          else {
               write-host("############## There is already a Database with the Name $iotDatabaseName.")
          }

          #Printing the variables
          Write-Host "dataConMappingName: $deviceTwinDataConMappingName"
          Write-Host "clusterLocation: $clusterLocation"
          Write-Host "dataconnectionName: $deviceTwinDataconnectionName"
          Write-Host "iotDatabaseName: $iotDatabaseName"
          Write-Host "clusterName: $clusterName"
          Write-Host "resourceGroupName: $resourceGroupName"
 

          #Data Connection
          ##checking if Name exists.
          if ((Test-AzKustoDataConnectionNameAvailability -ClusterName $clusterName -DatabaseName $iotDatabaseName -ResourceGroupName $resourceGroupName -Name $deviceTwinDataconnectionName -Type Microsoft.Kusto/Clusters/Databases/dataConnections).NameAvailable) {
               New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $iotDatabaseName -DataConnectionName $deviceTwinDataconnectionName -Location $clusterLocation -Kind "EventHub" -EventHubResourceId $deviceTwinEventHubResourceId -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "DeviceTwin" -MappingRuleName $deviceTwinDataConMappingName
               Write-Host "############## Created Data Connection."
          }
          else {
               write-host("############## There is already a Data conection with the Name $deviceTwinDataconnectionName.")
          }
     }
 
}
catch {
     Write-Host("An Error occured.")
     Write-Host($_)
}





    








