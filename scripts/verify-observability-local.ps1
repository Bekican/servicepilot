$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot `
    "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot `
    ".env.staging.local"
$correlationId = "collector-drill-" `
    + [Guid]::NewGuid().ToString("N").Substring(0, 12)

function Invoke-Compose
{
    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        @args

    if ($LASTEXITCODE -ne 0)
    {
        throw "Docker Compose command failed."
    }
}

if (-not (Test-Path -LiteralPath $environmentFile))
{
    throw "Create .env.staging.local before this drill."
}

Push-Location $repositoryRoot
try
{
    Invoke-Compose up -d --build --force-recreate `
        otel-collector migrator api worker

    $probeCommand = "wget -q -O /dev/null " `
        + "--header 'X-Correlation-ID: $correlationId' " `
        + "http://api:8080/api/auth/me || true"
    Invoke-Compose exec -T caddy sh -c $probeCommand
    Start-Sleep -Seconds 15

    $collectorLogs = Invoke-Compose logs --no-color `
        otel-collector | Out-String

    $requiredValues = @(
        "service.name: Str(ServicePilot.Api)",
        "service.name: Str(ServicePilot.Worker)",
        "service.name: Str(ServicePilot.Migrator)",
        "Name           : GET api/auth/me",
        "servicepilot.correlation_id: Str($correlationId)",
        "Name           : reminder.process_due",
        "Name           : retention.run",
        "Name           : database.migrate"
    )

    foreach ($value in $requiredValues)
    {
        if ($collectorLogs -notmatch [regex]::Escape($value))
        {
            throw "Collector output is missing: $value"
        }
    }

    $prohibitedValues = @(
        "commandText:",
        "RequestPath:",
        "url.path:",
        "url.query:",
        "http.target:"
    )

    foreach ($value in $prohibitedValues)
    {
        if ($collectorLogs -match [regex]::Escape($value))
        {
            throw "Collector output contains unsafe field: $value"
        }
    }

    Invoke-Compose stop otel-collector
    $status = curl.exe `
        --insecure `
        --silent `
        --output NUL `
        --write-out "%{http_code}" `
        https://localhost:8443/ops/api/ready

    if ($status -ne "200")
    {
        throw "API returned $status while Collector was down."
    }
}
finally
{
    Invoke-Compose start otel-collector
    Pop-Location
}

Write-Host `
    "Collector logs, traces, sanitization and fail-open check passed."
