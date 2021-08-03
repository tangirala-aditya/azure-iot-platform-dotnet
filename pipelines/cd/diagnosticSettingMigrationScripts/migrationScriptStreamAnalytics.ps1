﻿param(
    [string] $resourceGroupName,
    [string] $subscriptionId,
    [string] $applicationCode
)

Install-Module -Name Az.StreamAnalytics -Force

function createDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$saJobName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/Microsoft.StreamAnalytics/streamingjobs/$saJobName"
    $workSpaceID = "/subscriptions/$subscriptionId/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/$loganalyticsName"
    Write-Host $resourceId
    Write-Host $subscriptionId
    $existingDiagSetting = Get-AzDiagnosticSetting -ResourceId $resourceId
    if($existingDiagSetting)
    {
    if($existingDiagSetting.WorkspaceId.Split('/')[8] -eq $loganalyticsName)
    {
    Write-Host "Can't create Diagnostics Settings due to the loganalytics workspace is already in use with another Diagnostic Settings of this device."
    return
    }
    }
    Write-Host "Creating Diagnostic Settings"
    Set-AzDiagnosticSetting -Name newDiag -ResourceId $resourceId -Category Execution, Authoring -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getSAJobListandCreateDiagnosticSettings([string]$resourceGroupName, [string]$subscriptionId, [string]$applicationCode) {
    $saJobList = Get-AzStreamAnalyticsJob -ResourceGroupName $resourceGroupName
    $splitRG = $resourceGroupName.Split('-')
    $loganalyticsName = -join ($applicationCode, "-loganalyticsws-", $splitRG[3])

    ForEach($item in $saJobList)
    {
    Write-Host $item.JobName
    createDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -saJobName $item.JobName -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}

getSAJobListandCreateDiagnosticSettings -resourceGroupName $resourceGroupName -subscriptionId $subscriptionId -applicationCode $applicationCode