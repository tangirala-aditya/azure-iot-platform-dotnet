param(
    [string] $resourceGroupName,
    [string] $subscriptionId,
    [string] $applicationCode
)


function createDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$eventHubNameSpaceName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.EventHub/namespaces/$eventHubNameSpaceName"
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
    Set-AzDiagnosticSetting -Name $diagnosticSetting -ResourceId $resourceId -Category ArchiveLogs, OperationalLogs, AutoScaleLogs, KafkaUserErrorLogs,KafkaCoordinatorLogs -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getIoTHubListandCreateDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$applicationCode) {
    $eventHubNameSpaceList = Get-AzEventHubNamespace -ResourceGroupName $resourceGroupName
    $splitRG = $resourceGroupName.Split('-')
    $loganalyticsName = -join ($applicationCode, "-loganalyticsws-", $splitRG[3])
    Write-Host $loganalyticsName

    ForEach($item in $eventHubNameSpaceList)
    {
    Write-Host $item.Name
        createDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -eventHubNameSpaceName $item.Name -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}


getIoTHubListandCreateDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -applicationCode $applicationCode
