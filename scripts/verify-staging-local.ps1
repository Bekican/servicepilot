$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"
$webDirectory = Join-Path (Join-Path $repositoryRoot "apps") "web"

function Assert-LastExitCode([string] $step)
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "$step failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForHttps(
    [string] $uri,
    [int] $timeoutSeconds = 120)
{
    $curlCommand = Get-Command curl.exe `
        -ErrorAction SilentlyContinue
    if ($null -eq $curlCommand)
    {
        $curlCommand = Get-Command curl
    }

    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        & $curlCommand.Source `
            --fail `
            --insecure `
            --silent `
            --show-error `
            $uri | Out-Null

        if ($LASTEXITCODE -eq 0)
        {
            return
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for $uri."
}

function Get-CaddyRootFingerprint
{
    $output = docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        exec -T caddy `
        sha256sum `
        /data/caddy/pki/authorities/local/root.crt
    Assert-LastExitCode "Caddy root fingerprint"
    return ($output -split "\s+")[0]
}

if (-not (Test-Path -LiteralPath $environmentFile))
{
    throw "Run scripts/start-staging-local.ps1 before this verification."
}

$suffix = (Get-Date).ToUniversalTime().ToString("yyyyMMddHHmmss") `
    + "-" `
    + [Guid]::NewGuid().ToString("N").Substring(0, 6)
$env:STAGING_SMOKE_SUFFIX = $suffix.ToLowerInvariant()
$env:STAGING_SMOKE_PASSWORD = "StagingSmoke123!"
$env:SERVICEPILOT_STAGING_URL = "https://localhost:8443"
$env:SERVICEPILOT_STAGING_MAILPIT_URL = `
    "https://localhost:8443"

Push-Location $repositoryRoot
try
{
    Wait-ForHttps "https://localhost:8443/login"
    Wait-ForHttps "https://localhost:8443/ops/api/ready"

    Push-Location $webDirectory
    try
    {
        npx playwright test `
            --config playwright.staging.config.ts `
            --grep "creates durable staging data through HTTPS"
        Assert-LastExitCode "Staging data creation smoke test"
    }
    finally
    {
        Pop-Location
    }

    $fingerprintBeforeRestart = Get-CaddyRootFingerprint

    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        restart postgres mailpit api worker web caddy
    Assert-LastExitCode "Staging runtime restart"

    Wait-ForHttps "https://localhost:8443/login"
    Wait-ForHttps "https://localhost:8443/ops/api/ready"
    Wait-ForHttps "https://mailpit.localhost:8443"

    $fingerprintAfterRestart = Get-CaddyRootFingerprint
    if ($fingerprintBeforeRestart -ne $fingerprintAfterRestart)
    {
        throw "Caddy local CA changed after the runtime restart."
    }

    Push-Location $webDirectory
    try
    {
        npx playwright test `
            --config playwright.staging.config.ts `
            --grep "keeps staging data after the runtime restart"
        Assert-LastExitCode "Staging persistence smoke test"
    }
    finally
    {
        Pop-Location
    }
}
finally
{
    Pop-Location
}

Write-Host "Local staging smoke and persistence verification passed."
