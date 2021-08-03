param(
    [string] $resourceGroupName,
    [string] $subscriptionId
)


function createDiagnosticSettings([string]$rgName, [string]$subscriptionId, [string]$eventHubNameSpaceName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$rgName/providers/Microsoft.EventHub/namespaces/$eventHubNameSpaceName"
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
    Set-AzDiagnosticSetting -Name newDiag -ResourceId $resourceId -Category ArchiveLogs, OperationalLogs, AutoScaleLogs, KafkaUserErrorLogs -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getIoTHubListandCreateDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId) {
    $eventHubNameSpaceList = Get-AzEventHubNamespace -ResourceGroupName $resourceGroupName
    $splitRG = $resourceGroupName.Split('-')
    $loganalyticsName = -join ("acshyd", "-loganalyticsws-", $splitRG[3])

    ForEach($item in $eventHubNameSpaceList)
    {
    Write-Host $item.Name
        createDiagnosticSettings -resourceGroupName "rg-iot-acs-dev" -subscriptionId "c36fb2f8-f98d-40d0-90a9-d65e93acb428" -eventHubNameSpaceName $item.Name -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}


getIoTHubListandCreateDiagnosticSettings resourceGroupName $resourceGroupName -subscriptionId $subscriptionId