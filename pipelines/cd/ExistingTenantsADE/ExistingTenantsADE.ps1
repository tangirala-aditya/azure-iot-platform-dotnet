Connect-AzAccount
Set-AzContext -SubscriptionId "f24c7600-f18b-4d5e-a974-0f6af4f7ce31"

$resourceGroupName = "sahRG"
$clusterName = "newcluster001"
$storageAccountName = "gokudbz"

Install-Module -Name Az.Kusto -Force
Install-Module -Name AzTable -Force
$ct = (Get-AzStorageAccount -ResourceGroupName "cloud-shell-storage-centralindia" -Name $storageAccountName).Context
$ty = (Get-AzStorageTable -Name "tenant" -Context $ct).CloudTable
$item=(Get-AzTableRow -table $ty -CustomFilter 'IsIotHubDeployed eq true')[0]


Foreach($item in $iothubarray)  
   {  
        #create db and check db 
        $databaseName = "IoT-"+ $item.TenantId
        $mappingName = "'TelemetryEvents_JSON_Mapping-"+ $item.TenantId+"'"
        $dataconnectionName = "TelemetryDataConnect-" + $item.TenantId.Split("-")[0]
        $IotHubResourceId = (Get-AzIotHub -ResourceGroupName $resourceGroupName -Name $item.IotHubName).Id
        $locationCluster = (Get-AzKustoCluster -Name $clusterName -ResourceGroupName $resourceGroupName).Location
        New-AzKustoDatabase -ResourceGroupName $resourceGroupName -ClusterName $clusterName -Name $databaseName -SoftDeletePeriod 10:00:00:00 -HotCachePeriod 1:00:00:00 -Kind ReadWrite -Location $locationCluster
        Get-AzKustoDatabase -ClusterName $clusterName -ResourceGroupName $resourceGroupName -Name $databaseName  

        #change the names in the script fiel for mapping Name
        (Get-Content -path C:\Users\sahith.gangineni\Desktop\script.txt -Raw) -replace 'MAPPINGNAME', $mappingName | Set-Content -Path C:\Users\sahith.gangineni\Desktop\script.txt 


        #remove and reisntall pkmngr and install packages
        Unregister-PackageSource -Name nuget.org
        Register-PackageSource -Name nuget.org -Location https://www.nuget.org/api/v2 -ProviderName NuGet
        Install-Package Microsoft.Azure.Kusto.Tools -RequiredVersion 5.1.0 -Destination "C:\" -Force
        $packagesRoot = "C:\Microsoft.Azure.Kusto.Tools\Tools"


        $clusterUrl = "https://newcluster001.eastus.kusto.windows.net"
        $applicationId = "b34faa19-7f22-4feb-8e66-8cc124671b39"
        $applicationKey = "7j941C~QR~NK9j_TAXU.tgAvFE8tR6lgqK"
        $authority = "7896c5aa-71b4-47c3-98d4-e707bdd7462f"

        $connStr = "Data Source="+ $clusterUrl+ ";Initial Catalog="+ $databaseName+ ";Application Client Id="+ $applicationId+ ";Application Key="+ $applicationKey+ ";AAD Federated Security=True;dSTS Federated Security=False;Authority Id="+ $authority
        cd C:\Microsoft.Azure.Kusto.Tools.5.1.0\tools
        .\Kusto.Cli.exe $connStr -script:"C:\Users\sahith.gangineni\Desktop\script.txt"

         #REVERT change the names in the script fiel for mapping Name
        (Get-Content -path C:\Users\sahith.gangineni\Desktop\script.txt -Raw) -replace $mappingName, 'MAPPINGNAME' | Set-Content -Path C:\Users\sahith.gangineni\Desktop\script.txt 

        #Data Connection
        New-AzKustoDataConnection -ResourceGroupName $resourceGroupName -ClusterName $clusterName -DatabaseName $databaseName -DataConnectionName $dataconnectionName -Kind "IotHub" -IotHubResourceId $IotHubResourceId -SharedAccessPolicyName "iothubowner" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Telemetry" -MappingRuleName $mappingName -Location $locationCluster
        
        
        #New-AzKustoDataConnection -ResourceGroupName "sahRG" -ClusterName "newcluster001" -DatabaseName "IoT-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -DataConnectionName "TelemetryDataConnect-aa1b1d9b"  -Kind "IotHub" -IotHubResourceId "/subscriptions/f24c7600-f18b-4d5e-a974-0f6af4f7ce31/resourceGroups/sahRG/providers/Microsoft.Devices/IotHubs/iothub-30555" -SharedAccessPolicyName "myiothubpolicy" -DataFormat "JSON" -ConsumerGroup '$Default' -TableName "Events" -MappingRuleName "TelemetryEvents_JSON_Mapping-aa1b1d9b-797f-4bf4-b0ff-96a09f2701b6" -Location "East US"

   }   





