param(
    [string]$ApiBaseUrl = "http://localhost:5267"
)

$ErrorActionPreference = "Stop"
$healthUrl = "$ApiBaseUrl/health/ready"
$maximumAttempts = 60

function Invoke-IdempotentPost
{
    param(
        [string]$Uri,
        [hashtable]$Headers,
        [string]$Body
    )

    try
    {
        Invoke-RestMethod `
            -Uri $Uri `
            -Method Post `
            -Headers $Headers `
            -ContentType "application/json; charset=utf-8" `
            -Body $Body | Out-Null
    }
    catch
    {
        if ($_.Exception.Response.StatusCode.value__ -ne 409)
        {
            throw
        }
    }
}

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
    timeZoneId = "Europe/Istanbul"
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

$login =
    Invoke-RestMethod `
    -Uri "$ApiBaseUrl/api/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body $loginBody

$headers = @{
    Authorization = "Bearer $($login.accessToken)"
}

$customers =
    @(
        (
            Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/customers" `
            -Method Get `
            -Headers $headers
        ) | ForEach-Object { $_ }
    )

$demoCustomers = @(
    @{
        type = "Individual"
        firstName = "Ahmet"
        lastName = "Yılmaz"
        companyName = $null
        contactPerson = $null
        email = "ahmet.yilmaz@servicepilot.local"
        phone = "+905551110001"
    },
    @{
        type = "Individual"
        firstName = "Ayşe"
        lastName = "Demir"
        companyName = $null
        contactPerson = $null
        email = "ayse.demir@servicepilot.local"
        phone = "+905551110002"
    },
    @{
        type = "Company"
        firstName = $null
        lastName = $null
        companyName = "Atlas Yönetim"
        contactPerson = "Ömer Faruk"
        email = "operasyon@atlas.example.com"
        phone = "+905551110003"
    }
)

foreach ($candidate in $demoCustomers)
{
    if ($customers.email -notcontains $candidate.email)
    {
        Invoke-IdempotentPost `
            -Uri "$ApiBaseUrl/api/customers" `
            -Headers $headers `
            -Body ($candidate | ConvertTo-Json)
    }
}

$services =
    @(
        (
            Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/services" `
            -Method Get `
            -Headers $headers
        ) | ForEach-Object { $_ }
    )

$demoServices = @(
    @{ name = "Kombi Bakımı"; defaultDurationMinutes = 60 },
    @{ name = "Elektrik Tesisatı"; defaultDurationMinutes = 90 },
    @{ name = "Genel Arıza Tespiti"; defaultDurationMinutes = 45 }
)

foreach ($candidate in $demoServices)
{
    if ($services.name -notcontains $candidate.name)
    {
        Invoke-IdempotentPost `
            -Uri "$ApiBaseUrl/api/services" `
            -Headers $headers `
            -Body ($candidate | ConvertTo-Json)
    }
}

$customers =
    @(
        (
            Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/customers" `
            -Method Get `
            -Headers $headers
        ) | ForEach-Object { $_ }
    )
$services =
    @(
        (
            Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/services" `
            -Method Get `
            -Headers $headers
        ) | ForEach-Object { $_ }
    )
$appointments =
    @(
        (
            Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/appointments" `
            -Method Get `
            -Headers $headers
        ) | ForEach-Object { $_ }
    )

if ($appointments.Count -eq 0)
{
    $firstStart = [DateTimeOffset]::UtcNow.AddHours(1)
    $firstStart =
        $firstStart.AddMinutes(
            15 - ($firstStart.Minute % 15))
    $firstStart =
        $firstStart.AddSeconds(-$firstStart.Second)
    $firstStart =
        $firstStart.AddMilliseconds(-$firstStart.Millisecond)

    for ($index = 0; $index -lt 2; $index++)
    {
        $start = $firstStart.AddHours(2 * $index)
        $duration =
            [int]$services[$index].defaultDurationMinutes
        $body = @{
            customerId = $customers[$index].id
            serviceId = $services[$index].id
            technicianUserId = $null
            startAt = $start.ToString("o")
            endAt = $start.AddMinutes($duration).ToString("o")
        } | ConvertTo-Json

        Invoke-RestMethod `
            -Uri "$ApiBaseUrl/api/appointments" `
            -Method Post `
            -Headers $headers `
            -ContentType "application/json" `
            -Body $body | Out-Null
    }
}

Write-Host "Demo tenant is ready."
Write-Host "Organization: servicepilot-demo"
Write-Host "Email: owner@servicepilot.local"
Write-Host "Password: Demo1234!"
