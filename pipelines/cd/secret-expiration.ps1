param(
     [string] $sendgridAPIKey,
     [string] $destEmailAddress
)

function Send-ExpiringDetails {
    param(
        [string] $sendgridAPIKey,
        [string] $destEmailAddress
    )
    $FileName= "./ExpirationSecrets.txt"
    $subject="Needs Immediate Attention | Expiration details of App registration Secrets and Certificates"
    $fromEmailAddress="iotplatformnoreply@mmm.com"
    $appRegList= @( "95d3c662-23ea-4e2d-8d3d-ea2448706934", "68ca44b8-9e1b-46a0-b258-307f8b450218", "eb99f9dd-a07f-4674-8294-2c44eaa7f09f", "51c5ac78-b4fb-4772-9918-976b95f3d50f", "4c98e61d-61c3-423b-9eb1-f5bee4c3d719" )
    $expiredDetails = @()
    $currDate = (Get-Date).AddDays(60).ToString("yyyy-MM-ddThh:mm:ssK")

    foreach($appReg in $appRegList)
    {
    $appRegObj = az ad app list --app-id $appReg | ConvertFrom-Json 
        if(($appRegObj.passwordCredentials.Count) -ge 1)
        {
            foreach($password in $appRegObj.passwordCredentials)
            {
                if($password.endDate -le $currDate)
                    {
                        $expSecret = [PSCustomObject]@{
                        Name = $appRegObj.displayName
                        Type = "Secret"
                        keyId = $password.keyId
                        endDate = $password.endDate
                        }
                $expiredDetails += $expSecret
                    }
            }
        }

        if(($appRegObj.keyCredentials.Count) -ge 1)
        {
            foreach($key in $appRegObj.keyCredentials)
            {
                if($key.endDate -le $currDate)
                {
                    $expCert = [PSCustomObject]@{
                    Name = $appRegObj.displayName
                    Type = "Certificate"
                    keyId = $key.keyId
                    endDate = $key.endDate
                    }
                $expiredDetails += $expCert
                }
            }
        }
    }

    $arraycontent= &{
    Write-Output "-------------------------------------------------------------------------"
    Write-Output "Total number of expired Keys: $($expiredDetails.Count). Find more details below:"
    $expiredDetails
    Write-Output "-------------------------------------------------------------------------"
    } 
    $content = Out-String -InputObject $arraycontent
    Write-Output $content
    $content > $FileName
    #Convert File to Base64
    $EncodedFile = [System.Convert]::ToBase64String([IO.File]::ReadAllBytes($FileName))

    $headers = New-Object "System.Collections.Generic.Dictionary[[String],[String]]"
    $headers.Add("Authorization", "Bearer " + $sendgridAPIKey)
    $headers.Add("Content-Type", "application/json")

    $body = @{
    personalizations = @(
        @{
            to = @(
                    @{
                        email = $destEmailAddress
                    }
            )
        }
    )
    from = @{
        email = $fromEmailAddress
    }
    attachments = @(   
    @{
    content=$EncodedFile
    filename=$FileName
    disposition="attachment"

        }
    )
    subject = $subject
    content = @(
        @{
            type = "text/plain"
            value = "Please find the details of expired, near expiring secrets and certificates in the attachment`n The following is the link which guides you to create appregistration secret along with configuration changes to other dependent services `n https://skydrive3m.sharepoint.com/teams/Serenity-IoT-Community/Shared%20Documents/ClientSecretCreationAndConfigUpdation.docx?d=wb9e87120c35a47c4854a1db7e86de329  `n `n Note: This link is accessible to all 3M Team Members "
        }
    )
    }

    $bodyJson = $body | ConvertTo-Json -Depth 4
    $response = Invoke-RestMethod -Uri https://api.sendgrid.com/v3/mail/send -Method Post -Headers $headers -Body $bodyJson
}

Send-ExpiringDetails -sendgridAPIKey $sendgridAPIKey -destEmailAddress $destEmailAddress