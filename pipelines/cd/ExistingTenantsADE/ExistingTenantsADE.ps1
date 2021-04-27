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
     $clusterName = $applicationCode + "kusto" + $environmentCategory
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory

     #remove and reisntall pkmngr and install packages
     Unregister-PackageSource -Name nuget.org
     Register-PackageSource -Name nuget.org -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name Az.Kusto -Force
     Install-Module -Name AzTable -Force
     Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Destination $(System.DefualtWorkingDirectory) -Force

     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')


     Foreach ($iotHub in $iotHubArray) {  
          #create db and check db 
          $databaseName = "IoT-" + $iotHub.TenantId
          $mappingName = "'TelemetryEvents_JSON_Mapping-" + $iotHub.TenantId + "'"
          $dataconnectionName = "TelemetryDataConnect-" + $iotHub.TenantId.Split("-")[0]
          $IotHubResourceId = (Get-AzIotHub -ResourceGroupName $resourceGroupName -Name $iotHub.IotHubName).Id
          $clusterURI = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Uri
          $clusterLocation = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
          New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $databaseName -SoftDeletePeriod 10:00:00:00 -HotCachePeriod 1:00:00:00 -Kind ReadWrite -Location $clusterLocation
          Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $databaseName  

          #change the names in the script file for mapping Name
          (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace 'MAPPINGNAME', $mappingName | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop

          $connStr = "Data Source=" + $clusterURI + ";Initial Catalog=" + $databaseName + ";Application Client Id=" + $servicePrincipalId + ";Application Key=" + $servicePrincipalKey + ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id=" + $tenantId
          Microsoft.Azure.Kusto.Tools.5.1.0\Tools\Kusto.Cli.exe $connStr -script:".\pipelines\cd\ExistingTenantsADE\script.txt"

          #REVERT change the names in the script fiel for mapping Name
          (Get-Content -path .\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace $mappingName, 'MAPPINGNAME' | Set-Content -Path .\pipelines\cd\ExistingTenantsADE\script.txt -ErrorAction Stop

          #Data Connection
          New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Kind "IotHub" -IotHubResourceId $IotHubResourceId -SharedAccessPolicyName "iothubowner" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $mappingName.Split("'")[1] -Location $clusterLocation
     }   
}

catch {
     Write-Host("An Error occured")
     Write-Host($_)
}