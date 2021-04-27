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

     Install-Module -Name Az.Kusto -Force
     Install-Module -Name AzTable -Force
     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iothubarray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')[0]


     Foreach ($item in $iothubarray) {  
          #create db and check db 
          $databaseName = "IoT-" + $item.TenantId
          $mappingName = "'TelemetryEvents_JSON_Mapping-" + $item.TenantId + "'"
          $dataconnectionName = "TelemetryDataConnect-" + $item.TenantId.Split("-")[0]
          $IotHubResourceId = (Get-AzIotHub -ResourceGroupName $resourceGroupName -Name $item.IotHubName).Id
          $locationCluster = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
          New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $databaseName -SoftDeletePeriod 10:00:00:00 -HotCachePeriod 1:00:00:00 -Kind ReadWrite -Location $locationCluster
          Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $databaseName  

          #change the names in the script file for mapping Name
          (Get-Content -path D:\3m\azure-iot-platform-dotnet\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace 'MAPPINGNAME', $mappingName | Set-Content -Path D:\3m\azure-iot-platform-dotnet\pipelines\cd\ExistingTenantsADE\script.txt 


          #remove and reisntall pkmngr and install packages
          Unregister-PackageSource -Name nuget.org
          Register-PackageSource -Name nuget.org -Location https://www.nuget.org/api/v2 -ProviderName NuGet
          Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Destination "C:\" -Force
          $packagesRoot = "C:\Microsoft.Azure.Kusto.Tools\Tools"

          $clusterUrl = "https://newcluster001.eastus.kusto.windows.net"
          $applicationId = "b34faa19-7f22-4feb-8e66-8cc124671b39"
          $applicationKey = "7j941C~QR~NK9j_TAXU.tgAvFE8tR6lgqK"
          $authority = "7896c5aa-71b4-47c3-98d4-e707bdd7462f"

          $connStr = "Data Source=" + $clusterUrl + ";Initial Catalog=" + $databaseName + ";Application Client Id=" + $applicationId + ";Application Key=" + $applicationKey + ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id=" + $authority
          cd C:\Microsoft.Azure.Kusto.Tools.5.1.0\tools
          .\Kusto.Cli.exe $connStr -script:"C:\Users\sahith.gangineni\Desktop\script.txt"

          #REVERT change the names in the script fiel for mapping Name
          (Get-Content -path D:\3m\azure-iot-platform-dotnet\pipelines\cd\ExistingTenantsADE\script.txt -Raw) -replace $mappingName, 'MAPPINGNAME' | Set-Content -Path D:\3m\azure-iot-platform-dotnet\pipelines\cd\ExistingTenantsADE\script.txt 

          #Data Connection
          New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Kind "IotHub" -IotHubResourceId $IotHubResourceId -SharedAccessPolicyName "iothubowner" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $mappingName -Location $locationCluster
     }   

}

catch {
     Write-Host("An Error occured")
     Write-Host($_)
}


#New-AzKustoDataConnection -ResourceGroupName "sahRG" -ClusterName "newcluster001" -DatabaseName "IoT-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -DataConnectionName "TelemetryDataConnect-aa1b1d9b"  -Kind "IotHub" -IotHubResourceId "/subscriptions/f24c7600-f18b-4d5e-a974-0f6af4f7ce31/resourceGroups/sahRG/providers/Microsoft.Devices/IotHubs/iothub-30555" -SharedAccessPolicyName "myiothubpolicy" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Events" -MappingRuleName "TelemetryEvents_JSON_Mapping-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -Location "East US"
# New-AzKustoDataConnection -ResourceGroupName "sahRG" -ClusterName "newcluster001" -DatabaseName "IoT-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -DataConnectionName "TelemetryDataConnect-aa1b1d9b"  -Kind "IotHub" -IotHubResourceId "/subscriptions/f24c7600-f18b-4d5e-a974-0f6af4f7ce31/resourceGroups/sahRG/providers/Microsoft.Devices/IotHubs/iothub-30555" -SharedAccessPolicyName "myiothubpolicy" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName "TelemetryEvents_JSON_Mapping-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -Location "East US"
