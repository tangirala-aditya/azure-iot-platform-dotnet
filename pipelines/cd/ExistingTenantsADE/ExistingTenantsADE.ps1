# Connect-AzAccount
# Set-AzContext -SubscriptionId "f24c7600-f18b-4d5e-a974-0f6af4f7ce31"

param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroup,
     [string] $servicePrincipalId, 
     [string] $servicePrincipalKey, 
     [string] $tenantId
)

try {
     $resourceGroupName = $resourceGroup
     Write-Host $resourceGroupName
     $clusterName = $applicationCode + "kusto" + $environmentCategory
     Write-Host $clusterName
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     Write-Host $storageAccountName
     
     #remove and reisntall pkmngr and install packages
     Register-PackageSource -Name MyNuGet -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name Az.Kusto -Force
     Install-Module -Name AzTable -Force
     Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Destination . -Force

     ls
     Write-Host "Installed Kusto, AzTable and Kusto.Tools successfully."

     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     Write-Host $cloudTable
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     Write-Host $tableObject
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     Write-Host $iotHubArray

     Foreach ($iotHub in $iotHubArray) {  
          #create db and check db 
          $databaseName = "IoT-" + $iotHub.TenantId
          Write-Host $databaseName
          $mappingName = "'TelemetryEvents_JSON_Mapping-" + $iotHub.TenantId + "'"
          Write-Host $mappingName
          $dataconnectionName = "TelemetryDataConnect-" + $iotHub.TenantId.Split("-")[0]
          Write-Host $dataconnectionName
          $IotHubResourceId = (Get-AzIotHub -ResourceGroupName $resourceGroupName -Name $iotHub.IotHubName).Id
          Write-Host $IotHubResourceId
          $clusterURI = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Uri          
          Write-Host $clusterURI
          $clusterLocation = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
          Write-Host $clusterLocation
          New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $databaseName -SoftDeletePeriod 10:00:00:00 -HotCachePeriod 1:00:00:00 -Kind ReadWrite -Location $clusterLocation
          Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $databaseName  

          #change the names in the script file for mapping Name
          (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace 'MAPPINGNAME', $mappingName | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop
          Write-Host "Changed the path in the script file!"

          $connStr = "Data Source=" + $clusterURI + ";Initial Catalog=" + $databaseName + ";Application Client Id=" + $servicePrincipalId + ";Application Key=" + $servicePrincipalKey + ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id=" + $tenantId
          Write-Host $connStr
          Microsoft.Azure.Kusto.Tools.5.1.0\Tools\Kusto.Cli.exe $connStr -script:".\pipelines\cd\ExistingTenantsADE\script.txt"
          Write-Host "Executed the Kusto Script!"

          #REVERT change the names in the script fiel for mapping Name
          (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace $mappingName, 'MAPPINGNAME' | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop
          Write-Host "Reverted the change in the script file!"

          #Data Connection
          $newMappingName = $mappingName.Split("'")[1]
          Write-Host $newMappingName
          Write-Host $clusterLocation
          Write-Host $IotHubResourceId
          Write-Host $dataconnectionName
          Write-Host $databaseName
          Write-Host $clusterName
          Write-Host $resourceGroupName
          
          New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Kind "IotHub" -IotHubResourceId $IotHubResourceId -SharedAccessPolicyName "iothubowner" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $newMappingName -Location $          New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Kind "IotHub" -IotHubResourceId $IotHubResourceId -SharedAccessPolicyName "iothubowner" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $newMappingName -Location $clusterLocation

          Write-Host "Created DataConenction!"
     }   
}

catch {
     Write-Host("An Error occured")
     Write-Host($_)
}