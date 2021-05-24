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
          $eventhubNamespace="telemetry-eventhub-" + $iotTenantId.SubString(0,8)
          $eventhubName="$iotTenantId-telemetry"
          if((Test-AzEventHubName -Namespace $eventhubNamespace).NameAvailable){
               New-AzEventHubNamespace -ResourceGroupName $resourceGroupName -Name $eventhubNamespace -Location $location                   
          }
          else {
               Write-Host "############## EventHub NameSpace Already $eventhubNamespace." 
          }
          #Place the EventHub Namespace primary connectionstting => appConfiguration
          $connectionString=Get-AzEventHubKey -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -AuthorizationRuleName RootManageSharedAccessKey
          az appconfig kv set --name $appConfigurationName --key "tenant:$iotTenantId​:telemetryHubConn" --value $connectionString.PrimaryConnectionString  --yes
          $isEventHubExists=Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $eventhubName -ErrorAction Ignore
          if($isEventHubExists  -eq $null){
               Write-Host "############## Creating EventHub $eventhubName" 
               New-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $eventhubName -MessageRetentionInDays 1
          }
          else { 
               Write-Host "############## EventHub Already exists $eventhubName." 
          }
		$eventHubResourceId = (Get-AzEventHub -ResourceGroupName $resourceGroupName -NamespaceName $eventhubNamespace -EventHubName $eventhubName).Id
          $IotHubName = $iotHub.IotHubName
          Write-host("############## Creating required for $IotHubName.")
          #create and check db 
          $databaseName = "IoT-" + $iotHub.TenantId
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
     }
 
}

 

catch {
     Write-Host("An Error occured.")
     Write-Host($_)
}





    








