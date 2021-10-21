param(
     [string] $applicationCode,
     [string] $environmentCategory,
     [string] $resourceGroupName,
     [string] $servicePrincipalId, 
     [string] $servicePrincipalKey, 
     [string] $tenantId,
     [string] $subscriptionId,
     [string] $location,
     [string] $storageAccountName,
     [string] $appConfigurationName,
     [string] $keyvaultName
)

function New-GrafanaApiKey {
     param(
          [string] $grafanabaseurl,
          [string] $keyvaultName,
          [string] $tenantId,
          [string] $orgId
     )
     $uri = $grafanabaseurl + "/api/auth/keys"

     $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
     $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "admin", "admin")))
     $headers.Add("Authorization", "Basic " + $base64AuthInfo)
     $headers.Add("Content-Type", "application/json")
     $headers.Add("X-Grafana-Org-Id", $orgId)
     $keyName = New-Guid
     $orgKeyName = "Grafana--"+ $tenantId +"--APIKey"

     $body = "{`"name`":`"$keyName`", `"role`": `"Admin`"}"
     Write-Host $body
     try {
          $response = Invoke-RestMethod -Uri $uri -Method 'POST' -Headers $headers -Body $body
          $response = ($response | ConvertTo-Json | ConvertFrom-Json)
          $apiKey =  ConvertTo-SecureString $response.key -AsPlainText -Force

          $keyResult=Set-AzKeyVaultSecret -VaultName $keyvaultName -Name $orgKeyName -SecretValue $apiKey
          Write-Host "Added Key...."

          return $response.key
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }
}

function New-GrafanaOrg {
     param(
          [string] $grafanabaseurl,
          [string] $appConfigurationName,
          [string] $tenantId
     )
     $uri = $grafanabaseurl + "/api/orgs"

     $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
     $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "admin", "admin")))
     $headers.Add("Authorization", "Basic " + $base64AuthInfo)
     $headers.Add("Content-Type", "application/json")
     $keyName = "tenant:"+$tenantId+":grafanaOrgId"

     $body = "{`"name`":`"$tenantId`"}"
     Write-Host "Body: " $body
     try {
          $response = Invoke-RestMethod -Uri $uri -Method 'POST' -Headers $headers -Body $body
          Write-Host $response
          $orgId = $response.orgId
          $keyResult=az appconfig kv set --name $appConfigurationName --key $keyName --value $orgId --yes
          return $orgId
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }
}

function Add-GrafanaAdminToOrg {
     param(
          [string] $grafanabaseurl,
          [string] $orgId
     )
     $orgUsersUri = $grafanabaseurl + "/api/orgs/" + $orgId + "/users"

     Write-Host "OrganizationUsersUri: " $orgUsersUri

     $orgHeaders = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
     $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "admin", "admin")))
     $orgHeaders.Add("Authorization", "Basic " + $base64AuthInfo)
     $orgHeaders.Add("Content-Type", "application/json")

     $orgUserbody = "{`"loginOrEmail`":`"admin`", `"role`": `"Admin`"}"
     Write-Host "OrganizationUserbody: " $orgUserbody
     try {
          $response = Invoke-RestMethod -Uri $orgUsersUri -Method 'POST' -Headers $orgHeaders -Body $orgUserbody
          $response = $response | ConvertTo-Json
          Write-Host "Admin User Added Successfully"
     }
     catch {
          Write-Host("An Error occured.")
          Write-Host($_)
     }
}

function Add-GrafanaUsers {
     param(
          [string] $grafanabaseurl,
          [string] $grafanaApiKey,
          [string] $tenantId
     )
     
     # Write-Host $grafanaApiKey

     $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
     $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f "admin", "admin")))
     $headers.Add("Authorization", "Basic " + $base64AuthInfo)
     $headers.Add("Content-Type", "application/json")

     $keyName = "Grafana-" + $tenantId + "-APIKey"

     $orgUsersUri = $grafanabaseurl + "/api/org/users"
     $globalUsersUri = $grafanabaseurl + "/api/admin/users"
     $defaultPassword = "admin"

     $orgHeaders = @{
          'Authorization' = "Bearer " + $grafanaApiKey
          'Content-Type'  = 'application/json'
     }

     $userCloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $userTableObject = (Get-AzStorageTable -Name "user" -Context $userCloudTable).CloudTable
     $userArray = (Get-AzTableRow -table $userTableObject -CustomFilter "(RowKey eq '$tenantId') and (Type ne 'Invited')")

     Foreach ($user in $userArray) {
          Write-Host "User PartitionKey: " $user.PartitionKey
          $userId = $user.PartitionKey;
          $userName = $user.Name;
          $role = "Viewer";
          #Write-Host $rolesArray
          if ($user.Roles -eq '["admin"]' -or $user.Roles -eq "['admin']") {
               Write-Host "Admin"
               $role = "Admin";
          }
          elseif ($user.Roles -eq '["contributor"]' -or $user.Roles -eq "['contributor']") {
               Write-Host "Editor"
               $role = "Editor";
          }
          else {
               Write-Host "Viewer"
               $role = "Viewer";
          }     

          $body = "{`"login`":`"$userId`", `"name`": `"$userName`", `"password`": `"$defaultPassword`"}"
          Write-Host "Body: " $body
          try {
               $response = Invoke-RestMethod -Uri $globalUsersUri -Method 'POST' -Headers $headers -Body $body
               $response = $response | ConvertTo-Json
               Write-Host "User Added successfully to Global Users"
          }
          catch {
               Write-Host("An Error occured.")
               Write-Host($_)
          }


          $orgUserbody = "{`"loginOrEmail`":`"$userId`", `"role`": `"$role`"}"
          Write-Host "OrganizationUserBody: " $orgUserbody
          try {
               $response = Invoke-RestMethod -Uri $orgUsersUri -Method 'POST' -Headers $orgHeaders -Body $orgUserbody
               $response = $response | ConvertTo-Json
               Write-Host "User Added Successfully"
          }
          catch {
               Write-Host("An Error occured.")
               Write-Host($_)
          }

     }     
}

function New-Dashboards {
     param(
          [string] $applicationCode,
          [string] $environmentCategory,
          [string] $resourceGroup,
          [string] $servicePrincipalId, 
          [string] $servicePrincipalKey,
          [string] $subscriptionId, 
          [string] $tenantId,
          [string] $grafanabaseurl,
          [string] $apptenantId,
          [string] $grafanaApiKey,
          [string] $orgId
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
     $dashboardContent = $dashboardContent -replace '\{8\}' , $orgId


     $body = $dashboardContent | ConvertFrom-Json | ConvertTo-Json -Depth 32

     $uri = $grafanabaseurl + "/api/dashboards/db"
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
     $admindashboardContent = $admindashboardContent -replace '\{6\}' , ($applicationCode + "-eventhub-" + $environmentCategory)
     $admindashboardContent = $admindashboardContent -replace '\{7\}' , ($applicationCode + "-cosmos-" + $environmentCategory)
     $admindashboardContent = $admindashboardContent -replace '\{8\}' , ($tenantSubString + "-adm")
     $admindashboardContent = $admindashboardContent -replace '\{9\}' , ($tenantSubString + "-AdminDashboard")
     $admindashboardContent = $admindashboardContent -replace '\{10\}' , $orgId

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
          [string] $servicePrincipalId, 
          [string] $servicePrincipalKey,
          [string] $tenantId,
          [string] $grafanabaseurl,
          [string] $grafanaApiKey,
          [string] $location
     )

     # creation of Data Sources for Grafana Dashboards

     $dataSourceContent = Get-Content '.\pipelines\cd\GrafanaMigration\sample-dataexplorer-datasource-template.json' -raw 

     $dataSourceContent = $dataSourceContent -replace '\{0\}' , $servicePrincipalId
     $dataSourceContent = $dataSourceContent -replace '\{1\}' , $tenantId
     $dataSourceContent = $dataSourceContent -replace '\{2\}' , $servicePrincipalKey
     $dataSourceContent = $dataSourceContent -replace '\{3\}' , ("https://" + $applicationCode + "kusto" + $environmentCategory + ".$location.kusto.windows.net")


     $body = $dataSourceContent | ConvertFrom-Json | ConvertTo-Json -Depth 32

     $uri = $grafanabaseurl + "/api/datasources"
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

     $uri = $grafanabaseurl + "/api/datasources"
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
     $grafanabaseurl = "https://$applicationCode-aks-$environmentCategory.$location.cloudapp.azure.com/grafana"

     #remove and reisntall pkmngr and install packages
     #Register-PackageSource -Name MyNuGet -Location https://www.nuget.org/api/v2 -ProviderName NuGet
     Install-Module -Name AzTable -Force

     Write-Host "############## Installed AzTable successfully."

     az cloud set -n AzureCloud
     az login --service-principal -u $servicePrincipalId --password $servicePrincipalKey --tenant $tenantId --allow-no-subscriptions
     az account set --subscription $subscriptionId

     $cloudTable = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name $storageAccountName).Context
     $tableObject = (Get-AzStorageTable -Name "tenant" -Context $cloudTable).CloudTable
     $iotHubArray = (Get-AzTableRow -table $tableObject -CustomFilter 'IsIotHubDeployed eq true')
     
     Foreach ($iotHub in $iotHubArray) {
          $iotTenantId = $iotHub.TenantId  

          #Create a Org for Tenant
          $orgId = New-GrafanaOrg -grafanabaseurl $grafanabaseurl `
               -appConfigurationName $appConfigurationName `
               -tenantId $iotTenantId
			   
          if(![string]::IsNullOrWhiteSpace($orgId)){
          Write-Host "Created Org with Org Id:"+ $orgId

          # Add Admin User to Org
          Add-GrafanaAdminToOrg -grafanabaseurl $grafanabaseurl `
               -orgId $orgId

          Write-Host "Add Admin User to Org" $orgId

          # Generate API Key for Org
          $grafanaApiKey = New-GrafanaApiKey -grafanabaseurl $grafanabaseurl `
               -keyvaultName $keyvaultName `
               -tenantId $iotTenantId `
               -orgId $orgId

          Write-Host "Created APIKey for Org with Org Id:" $grafanaApiKey

          # Add Users to Org
          Add-GrafanaUsers -grafanabaseurl $grafanabaseurl `
               -grafanaApiKey $grafanaApiKey `
               -tenantId $iotTenantId
          
                    # Create DataSources required for Grafana Dashboards
          Write-Host "## Creating Data Sources"
          New-DataSources -applicationCode $applicationCode `
                          -environmentCategory $environmentCategory `
                          -servicePrincipalId $servicePrincipalId `
                          -servicePrincipalKey $servicePrincipalKey `
                          -tenantId $tenantId `
                          -grafanabaseurl $grafanabaseurl `
                          -grafanaApiKey $grafanaApiKey `
                          -location $location
          Write-Host "## Data Sources are created"

          # Create Main and Admin Dashboards for each tenant.
          New-Dashboards -applicationCode $applicationCode `
                          -environmentCategory $environmentCategory `
                          -resourceGroup $resourceGroupName `
                          -servicePrincipalId $servicePrincipalId `
                          -servicePrincipalKey $servicePrincipalKey `
                          -subscriptionId $subscriptionId `
                          -tenantId $tenantId `
                          -grafanabaseurl $grafanabaseurl `
                          -apptenantId $iotHub.TenantId `
                          -grafanaApiKey $grafanaApiKey `
                          -orgId $orgId
          Write-Host "Created Dashboard for Tenant:" + $iotTenantId       
     }
     }
}
catch {
     Write-Host("An Error occured.")
     Write-Host($_)
}


