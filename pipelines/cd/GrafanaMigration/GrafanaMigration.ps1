param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroup, 
     [string] $tenantId,
     [string] $location,
     [string] $servicePrincipalId,
     [string] $servicePrincipalKey

)
function New-GrafanaApiKey {
     param(
          [string] $grafanabaseurl,
          [string] $keyvaultName
     )
     $uri = $grafanabaseurl + "api/auth/keys"

     $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
     $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "admin", "admin")))
     $headers.Add("Authorization", "Basic " + $base64AuthInfo)
     $headers.Add("Content-Type", "application/json")
     $keyName = New-Guid

     $body = "{`"name`":`"$keyName`", `"role`": `"Admin`"}"
     Write-Host $body
     try {
          $response = Invoke-RestMethod -Uri $uri -Method 'POST' -Headers $headers -Body $body
          $response = $response | ConvertTo-Json
          $apiKey = $response.key
          $secret = Set-AzKeyVaultSecret -VaultName $keyvaultName -Name "Grafana--APIKey" -SecretValue $apiKey
          Write-Host $result.key
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }
}

function New-Dashboards {
     param(
          [string] $applicationCode,
          [string] $environmentCategory,
          [string] $resourceGroup,
          [string] $subscriptionId, 
          [string] $tenantId,
          [string] $grafanabaseurl,
          [string] $apptenantId,
          [string] $grafanaApiKey
     )

     # creation of Main Dashboard for Tenant

     $tenantSubString = $apptenantId.Split("-")[0]
     $appConfigurationName = $applicationCode + "-appconfig-" + $environmentCategory

     $dashboardContent = Get-Content '.\pipelines\cd\GrafanaMigration\sample-dashboard_template.json' -raw 

     $dashboardContent = $dashboardContent -replace '\{0\}' , $grafanabaseurl
     $dashboardContent = $dashboardContent -replace '\{1\}' , ($tenantSubString + "-adm/" + $tenantSubString + "-AdminDashboard")
     $dashboardContent = $dashboardContent -replace '\{2\}' , $subscriptionId
     $dashboardContent = $dashboardContent -replace '\{3\}' , $resourceGroup
     $dashboardContent = $dashboardContent -replace '\{4\}' , ($applicationCode + "-loganalyticsws-" + $environmentCategory)
     $dashboardContent = $dashboardContent -replace '\{5\}' , ("IoT-" + $apptenantId)
     $dashboardContent = $dashboardContent -replace '\{6\}' , $tenantSubString
     $dashboardContent = $dashboardContent -replace '\{7\}' , ($tenantSubString + "-Dashboard")


     $body = $dashboardContent | ConvertFrom-Json | ConvertTo-Json -Depth 32

     $uri = $grafanabaseurl + "grafana/api/dashboards/db"
     $headers = @{
          'Authorization' = "Bearer " + $grafanaApiKey
          'Content-Type'  = 'application/json'
     }

     try {
          Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $body
          az appconfig kv set --name $appConfigurationName --key ("tenant:"+$iotTenantId+":grafanaUrl") --value ($tenantSubString + "/" + $tenantSubString + "-Dashboard") --yes
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }


     $admindashboardContent = Get-Content '.\pipelines\cd\GrafanaMigration\sample-admin-dashboard_template.json' -raw 

     $admindashboardContent = $admindashboardContent -replace '\{0\}' , $grafanabaseurl
     $admindashboardContent = $admindashboardContent -replace '\{1\}' , ($tenantSubString + "/" + $tenantSubString + "-Dashboard")
     $admindashboardContent = $admindashboardContent -replace '\{2\}' , $subscriptionId
     $admindashboardContent = $admindashboardContent -replace '\{3\}' , $resourceGroup
     $admindashboardContent = $admindashboardContent -replace '\{4\}' , ($applicationCode + "-loganalyticsws-" + $environmentCategory)
     $admindashboardContent = $admindashboardContent -replace '\{5\}' , ("iothub-" + $tenantSubString)
     $admindashboardContent = $admindashboardContent -replace '\{6\}' , ($applicationCode + "-eventhub-" + $environmentCategoryacsagic)
     $admindashboardContent = $admindashboardContent -replace '\{7\}' , ($applicationCode + "-cosmos-" + $environmentCategoryacsagic)
     $admindashboardContent = $admindashboardContent -replace '\{8\}' , ($tenantSubString + "-adm")
     $admindashboardContent = $admindashboardContent -replace '\{9\}' , ($tenantSubString + "-AdminDashboard")

     $adminbody = $admindashboardContent | ConvertFrom-Json | ConvertTo-Json -Depth 32


     try {
          Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $adminbody
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }

}

function New-DataSources {
     param(
          [string] $applicationCode,
          [string] $environmentCategory,
          [string] $tenantId,
          [string] $grafanabaseurl,
          [string] $grafanaApiKey,
          [string] $servicePrincipalId,
          [string] $servicePrincipalKey

     )

     # creation of Data Sources for Grafana Dashboards

     $dataSourceContent = Get-Content '.\pipelines\cd\GrafanaMigration\sample-dataexplorer-datasource-template.json' -raw 

     $dataSourceContent = $dataSourceContent -replace '\{0\}' , $servicePrincipalId
     $dataSourceContent = $dataSourceContent -replace '\{1\}' , $tenantId
     $dataSourceContent = $dataSourceContent -replace '\{2\}' , $servicePrincipalKey
     $dataSourceContent = $dataSourceContent -replace '\{3\}' , ("https://" + $applicationCode + "kusto" + $environmentCategory + ".centralus.kusto.windows.net")


     $body = $dataSourceContent | ConvertFrom-Json | ConvertTo-Json -Depth 32

     $uri = $grafanabaseurl + "api/datasources"
     $headers = @{
          'Authorization' = "Bearer " + $grafanaApiKey
          'Content-Type'  = 'application/json'
     }

     try {
          Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $body
          Write-Host "## Created Azure Data Explorer Monitor Data Source"
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }

     $monitorDataSource = Get-Content '.\pipelines\cd\GrafanaMigration\sample-azuremonitor-datasource-template.json' -raw 

     $monitorDataSource = $monitorDataSource -replace '\{0\}' , $servicePrincipalId
     $monitorDataSource = $monitorDataSource -replace '\{1\}' , $tenantId
     $monitorDataSource = $monitorDataSource -replace '\{2\}' , $servicePrincipalKey


     $monitorDataSourceBody = $monitorDataSource | ConvertFrom-Json | ConvertTo-Json -Depth 32

     $uri = $grafanabaseurl + "api/datasources"
     $headers = @{
          'Authorization' = "Bearer " + $grafanaApiKey
          'Content-Type'  = 'application/json'
     }

     try {
          Invoke-RestMethod -Uri $uri -Method Post -Headers $headers -Body $monitorDataSourceBody
          Write-Host "## Created Azure Monitor Data Source"
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }
}

try {
            
     $resourceGroupName = $resourceGroup
     $storageAccountName = $applicationCode + "storageacct" + $environmentCategory
     $keyvaultName = $applicationCode + "-keyvault-" + $environmentCategory
     $grafanabaseurl = "https://$applicationCode-aks-$environmentCategory.$location.cloudapp.azure.com/grafana/"

     #remove and reisntall pkmngr and install packages
     #Register-PackageSource -Name MyNuGet -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name AzTable -Force

     Write-Host "############## Installed AzTable successfully."

     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     $grafanaApiKey = Get-AzKeyVaultSecret -VaultName $keyvaultName -Name "Grafana--APIKey" -AsPlainText
     Write-Host $grafanaApiKey

     # Create DataSources required for Grafana Dashboards
     Write-Host "## Creating Data Sources"
     New-DataSources -applicationCode $applicationCode -environmentCategory $environmentCategory -servicePrincipalId $servicePrincipalId -servicePrincipalKey $servicePrincipalKey -tenantId $tenantId -grafanabaseurl $grafanabaseurl -grafanaApiKey $grafanaApiKey
     Write-Host "## Data Sources are created"

     Foreach ($iotHub in $iotHubArray) {
          $iotTenantId = $iotHub.TenantId
          # Create Main and Admin Dashboards for each tenant.
          New-Dashboards -applicationCode $applicationCode -environmentCategory $environmentCategory -resourceGroup $resourceGroup -servicePrincipalId $servicePrincipalId -servicePrincipalKey $servicePrincipalKey -subscriptionId $subscriptionId -tenantId $tenantId -grafanabaseurl $grafanabaseurl -apptenantId $iotHub.TenantId -grafanaApiKey $grafanaApiKey
          Write-Host "Created Dashboard for Tenant:" + $iotTenantId        
     }
}
catch {
     Write-Host("An Error occured.")
     Write-Host($_)
}




