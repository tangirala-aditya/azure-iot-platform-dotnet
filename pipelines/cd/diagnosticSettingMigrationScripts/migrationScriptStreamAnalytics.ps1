param(
    [string] $rgName,
    [string] $subscriptionId
)

Install-Module -Name Az.StreamAnalytics -Force

function createDiagnosticSettings([string]$rgName, [string]$subscriptionId, [string]$saJobName, [string]$loganalyticsName) {
    $resourceId = "/subscriptions/$subscriptionId/resourceGroups/$rgName/providers/Microsoft.StreamAnalytics/streamingjobs/$saJobName"
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
    Set-AzDiagnosticSetting -Name newDiag -ResourceId $resourceId -Category Execution, Authoring -MetricCategory AllMetrics -Enabled $true -WorkspaceId $workSpaceID   
}


function getSAJobListandCreateDiagnosticSettings([string]$rgName, [string]$subscriptionId){
    $saJobList = Get-AzStreamAnalyticsJob -ResourceGroupName $rgName
    $splitRG = $rgName.Split('-')
    $loganalyticsName = -join ("acshyd", "-loganalyticsws-", $splitRG[3])

    ForEach($item in $saJobList)
    {
    Write-Host $item.JobName
    createDiagnosticSettings -rgName $rgName -subscriptionId $subscriptionId -saJobName $item.JobName -loganalyticsName $loganalyticsName
    Write-Host "======================================="
    }
}

getSAJobListandCreateDiagnosticSettings -rgName $rgName -subscriptionId $subscriptionId