param(
    [string] $rgName,
    [string] $subscriptionId
)


function createDiagnosticSettings([string]$rgName, [string]$subscriptionId, [string]$vaultName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$rgName/providers/Microsoft.KeyVault/vaults/$vaultName"
    $workSpaceID = "/subscriptions/$subscriptionId/resourceGroups/$rgName/providers/microsoft.operationalinsights/workspaces/$loganalyticsName"
    $existingDiagSetting = (Get-AzDiagnosticSetting -ResourceId $resourceId)
    if($existingDiagSetting)
    {
    if($existingDiagSetting.WorkspaceId.Split('/')[8] -eq $loganalyticsName)
    {
    Write-Host "Can't create Diagnostics Settings due to the loganalytics workspace is already in use with another Diagnostic Settings of this device."
    return
    }
    }
    Write-Host "Creating Diagnostic Settings"
    Set-AzDiagnosticSetting -Name newDiag -ResourceId $resourceId -Category AuditEvent -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getIoTHubListandCreateDiagnosticSettings([string]$rgName, [string]$subscriptionId){
    $kvList = Get-AzKeyVault -ResourceGroupName $rgName
    $splitRG = $rgName.Split('-')
    $loganalyticsName = -join ("acshyd", "-loganalyticsws-", $splitRG[3])

    ForEach($item in $kvList)
    {
    Write-Host $item.VaultName
    createDiagnosticSettings -rgName "rg-iot-acs-dev" -subscriptionId "c36fb2f8-f98d-40d0-90a9-d65e93acb428" -vaultName $item.VaultName -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}

getIoTHubListandCreateDiagnosticSettings -rgName $rgName -subscriptionId $subscriptionId