[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"
$stateDirectory = Join-Path $repositoryRoot ".deploy-state\staging-local"
$currentReleasePath = Join-Path $stateDirectory "current-release.json"
$previousReleasePath = Join-Path $stateDirectory "previous-release.json"
$previousEnvironmentPath = Join-Path $stateDirectory "previous.env"
$rollbackEnvironmentPath = Join-Path $stateDirectory "rollback-current.env"

function Assert-LastExitCode([string]$Step) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForHttps([string]$Uri, [int]$TimeoutSeconds = 120) {
    $curlCommand = Get-Command curl.exe -ErrorAction SilentlyContinue
    if ($null -eq $curlCommand) {
        $curlCommand = Get-Command curl
    }
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        & $curlCommand.Source --fail --insecure --silent --show-error $Uri | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return
        }
        Start-Sleep -Seconds 2
    }
    throw "Timed out waiting for $Uri."
}

if (-not (Test-Path $environmentFile) -or -not (Test-Path $previousEnvironmentPath)) {
    throw "No previous managed staging release is available for rollback."
}

$currentManifest = $null
if (Test-Path $currentReleasePath) {
    $currentManifest = Get-Content -Path $currentReleasePath -Raw
}
$previousManifest = $null
if (Test-Path $previousReleasePath) {
    $previousManifest = Get-Content -Path $previousReleasePath -Raw
}

Copy-Item -Path $environmentFile -Destination $rollbackEnvironmentPath -Force
Copy-Item -Path $previousEnvironmentPath -Destination $environmentFile -Force
$composePrevious = @("compose", "--env-file", $environmentFile, "-f", $composeFile)

try {
    Write-Host "Rolling application containers back. PostgreSQL and migrations are left unchanged..."
    & docker @composePrevious config --quiet
    Assert-LastExitCode "Previous Compose validation"

    & docker @composePrevious up -d --no-build --no-deps --force-recreate api worker web caddy
    Assert-LastExitCode "Application rollback"
    Wait-ForHttps "https://localhost:8443/login"
    Wait-ForHttps "https://localhost:8443/ops/api/ready"

    Copy-Item -Path $rollbackEnvironmentPath -Destination $previousEnvironmentPath -Force
    if ($null -ne $previousManifest) {
        $previousManifest | Set-Content -Path $currentReleasePath -Encoding utf8
    }
    else {
        Remove-Item -Path $currentReleasePath -Force -ErrorAction SilentlyContinue
    }
    if ($null -ne $currentManifest) {
        $currentManifest | Set-Content -Path $previousReleasePath -Encoding utf8
    }

    Write-Host "Application rollback passed readiness checks. The database was not restored or migrated down."
}
catch {
    Write-Warning "Rollback failed. Returning the environment file to the release that was active before this command."
    Copy-Item -Path $rollbackEnvironmentPath -Destination $environmentFile -Force
    $composeCurrent = @("compose", "--env-file", $environmentFile, "-f", $composeFile)
    & docker @composeCurrent up -d --no-build --no-deps --force-recreate api worker web caddy
    throw
}
finally {
    Remove-Item -Path $rollbackEnvironmentPath -Force -ErrorAction SilentlyContinue
}
