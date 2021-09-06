param(
    [string] $resourceGroupName,
    [string] $subscriptionId,
    [string] $applicationCode
)


function createDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$vaultName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.KeyVault/vaults/$vaultName"
    $workSpaceID = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$loganalyticsName"
    $existingDiagSetting = Get-AzDiagnosticSetting -ResourceId $resourceId
    $splitRG = $resourceGroupName.Split('-')
    $diagnosticSetting = -join ($applicationCode, "-diagnosticsetting-", $splitRG[3])

    if($existingDiagSetting)
    {
    if($existingDiagSetting.WorkspaceId.Split('/')[8] -eq $loganalyticsName)
    {
    Write-Host "Can't create Diagnostics Settings due to the loganalytics workspace is already in use with another Diagnostic Settings of this device."
    return
    }
    }
    Write-Host "Creating Diagnostic Settings"
    Set-AzDiagnosticSetting -Name $diagnosticSetting -ResourceId $resourceId -Category AuditEvent -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getIoTHubListandCreateDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$applicationCode) {
    $kvList = Get-AzKeyVault -ResourceGroupName $resourceGroupName
    $splitRG = $resourceGroupName.Split('-')
    $loganalyticsName = -join ($applicationCode, "-loganalyticsws-", $splitRG[3])

    ForEach($item in $kvList)
    {
    Write-Host $item.VaultName
        createDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -vaultName $item.VaultName -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}

getIoTHubListandCreateDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -applicationCode $applicationCode