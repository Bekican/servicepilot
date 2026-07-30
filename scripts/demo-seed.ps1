param(
    [string]$ApiBaseUrl = "http://localhost:5267"
)

$ErrorActionPreference = "Stop"
$healthUrl = "$ApiBaseUrl/health/ready"
$maximumAttempts = 60

for ($attempt = 1; $attempt -le $maximumAttempts; $attempt++)
{
    try
    {
        Invoke-RestMethod -Uri $healthUrl -Method Get | Out-Null
        break
    }
    catch
    {
        if ($attempt -eq $maximumAttempts)
        {
            throw "ServicePilot API did not become ready."
        }

        Start-Sleep -Seconds 2
    }
}

$registerBody = @{
    organizationName = "ServicePilot Demo"
    organizationSlug = "servicepilot-demo"
    firstName = "Demo"
    lastName = "Owner"
    email = "owner@servicepilot.local"
    password = "Demo1234!"
} | ConvertTo-Json

try
{
    Invoke-RestMethod `
        -Uri "$ApiBaseUrl/api/auth/register" `
        -Method Post `
        -ContentType "application/json" `
        -Body $registerBody | Out-Null
}
catch
{
    if ($_.Exception.Response.StatusCode.value__ -ne 409)
    {
        throw
    }
}

$loginBody = @{
    organizationSlug = "servicepilot-demo"
    email = "owner@servicepilot.local"
    password = "Demo1234!"
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "$ApiBaseUrl/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody | Out-Null

Write-Host "Demo tenant is ready."
Write-Host "Organization: servicepilot-demo"
Write-Host "Email: owner@servicepilot.local"
Write-Host "Password: Demo1234!"
