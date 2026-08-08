$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"
$environmentExample = Join-Path $repositoryRoot `
    "deploy/staging/.env.local.example"

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

Push-Location $repositoryRoot
try
{
    if (-not (Test-Path -LiteralPath $environmentFile))
    {
        Copy-Item `
            -LiteralPath $environmentExample `
            -Destination $environmentFile
        Write-Host `
            "Created .env.staging.local from the local-only template."
    }

    $environmentContent = Get-Content -LiteralPath $environmentFile -Raw
    if ($environmentContent -notmatch "(?m)^SERVICEPILOT_BACKUP_IMAGE=")
    {
        Add-Content `
            -LiteralPath $environmentFile `
            -Value "SERVICEPILOT_BACKUP_IMAGE=servicepilot-backup:local"
        Write-Host `
            "Added the backup image contract to the existing local environment."
    }

    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        config --quiet
    Assert-LastExitCode "Staging Compose validation"

    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        run --rm --no-deps caddy `
        caddy validate `
        --config /etc/caddy/Caddyfile `
        --adapter caddyfile
    Assert-LastExitCode "Caddy validation"

    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        up --build --force-recreate -d
    Assert-LastExitCode "Local staging startup"

    Wait-ForHttps "https://localhost:8443/login"
    Wait-ForHttps "https://localhost:8443/ops/api/ready"
    Wait-ForHttps "https://localhost:8443/ops/web/ready"
    Wait-ForHttps "https://mailpit.localhost:8443"

    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        ps
    Assert-LastExitCode "Local staging status"
}
finally
{
    Pop-Location
}

Write-Host "Local staging is ready."
Write-Host "Web: https://localhost:8443"
Write-Host "API readiness: https://localhost:8443/ops/api/ready"
Write-Host "Web readiness: https://localhost:8443/ops/web/ready"
Write-Host "Mailpit: https://mailpit.localhost:8443"
Write-Host "HTTP redirect: http://localhost:8080"
Write-Host `
    "The certificate is issued by Caddy's local CA and may require local trust."
