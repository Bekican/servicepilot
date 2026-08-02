[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseManifest,
    [switch]$AllowMutableLocalImages,
    [switch]$SkipPull,
    [switch]$SkipRecoveryDrill
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"
$stateDirectory = Join-Path $repositoryRoot ".deploy-state\staging-local"
$currentReleasePath = Join-Path $stateDirectory "current-release.json"
$previousReleasePath = Join-Path $stateDirectory "previous-release.json"
$previousEnvironmentPath = Join-Path $stateDirectory "previous.env"
$candidateEnvironmentPath = Join-Path $stateDirectory "candidate.env"

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

function Get-EnvironmentValue([string]$Content, [string]$Name) {
    $match = [regex]::Match($Content, "(?m)^$([regex]::Escape($Name))=(.*)$")
    if (-not $match.Success) {
        throw "Missing $Name in .env.staging.local."
    }
    return $match.Groups[1].Value.Trim()
}

function Set-EnvironmentValue([string]$Content, [string]$Name, [string]$Value) {
    $pattern = "(?m)^$([regex]::Escape($Name))=.*$"
    if (-not [regex]::IsMatch($Content, $pattern)) {
        throw "Missing $Name in .env.staging.local."
    }
    return [regex]::Replace($Content, $pattern, "$Name=$Value")
}

function New-ManifestFromEnvironment([string]$Content, [string]$ReleaseId) {
    return [ordered]@{
        releaseId = $ReleaseId
        images = [ordered]@{
            api = Get-EnvironmentValue $Content "SERVICEPILOT_API_IMAGE"
            web = Get-EnvironmentValue $Content "SERVICEPILOT_WEB_IMAGE"
            worker = Get-EnvironmentValue $Content "SERVICEPILOT_WORKER_IMAGE"
            migrator = Get-EnvironmentValue $Content "SERVICEPILOT_MIGRATOR_IMAGE"
        }
    }
}

function Write-Utf8WithoutBom([string]$Path, [string]$Content) {
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

if (-not (Test-Path $environmentFile)) {
    throw "Missing .env.staging.local. Start local staging before deploying a release."
}
if (-not (Test-Path $ReleaseManifest)) {
    throw "Release manifest not found: $ReleaseManifest"
}

$manifest = Get-Content -Path $ReleaseManifest -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($manifest.releaseId)) {
    throw "The release manifest must contain releaseId."
}

$imageMap = [ordered]@{
    SERVICEPILOT_API_IMAGE = [string]$manifest.images.api
    SERVICEPILOT_WEB_IMAGE = [string]$manifest.images.web
    SERVICEPILOT_WORKER_IMAGE = [string]$manifest.images.worker
    SERVICEPILOT_MIGRATOR_IMAGE = [string]$manifest.images.migrator
}

foreach ($entry in $imageMap.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace($entry.Value)) {
        throw "The release manifest is missing image $($entry.Key)."
    }
    if (-not $AllowMutableLocalImages -and $entry.Value -notmatch '@sha256:[0-9a-fA-F]{64}$') {
        throw "Image $($entry.Value) is not pinned by digest. Mutable references require -AllowMutableLocalImages and are local-only."
    }
}

New-Item -ItemType Directory -Path $stateDirectory -Force | Out-Null
$currentEnvironment = Get-Content -Path $environmentFile -Raw
$candidateEnvironment = $currentEnvironment
foreach ($entry in $imageMap.GetEnumerator()) {
    $candidateEnvironment = Set-EnvironmentValue $candidateEnvironment $entry.Key $entry.Value
}
Write-Utf8WithoutBom $candidateEnvironmentPath $candidateEnvironment

$composeCandidate = @("compose", "--env-file", $candidateEnvironmentPath, "-f", $composeFile)
$deploymentStarted = $false

try {
    & docker @composeCandidate config --quiet
    Assert-LastExitCode "Candidate Compose validation"

    if (-not $SkipPull) {
        & docker @composeCandidate pull api web worker migrator
        Assert-LastExitCode "Candidate image pull"
    }

    if (-not $SkipRecoveryDrill) {
        & "$PSScriptRoot\verify-recovery-local.ps1"
    }

    Write-Host "Applying forward-only migrations with the candidate Migrator image..."
    & docker @composeCandidate run --rm migrator
    Assert-LastExitCode "Candidate database migration"

    Copy-Item -Path $environmentFile -Destination $previousEnvironmentPath -Force
    if (Test-Path $currentReleasePath) {
        Copy-Item -Path $currentReleasePath -Destination $previousReleasePath -Force
    }
    else {
        $previousManifest = New-ManifestFromEnvironment $currentEnvironment "pre-managed-local-release"
        $previousManifest | ConvertTo-Json -Depth 4 | Set-Content -Path $previousReleasePath -Encoding utf8
    }

    Copy-Item -Path $candidateEnvironmentPath -Destination $environmentFile -Force
    $deploymentStarted = $true

    $composeLive = @("compose", "--env-file", $environmentFile, "-f", $composeFile)
    & docker @composeLive up -d --no-build --no-deps --force-recreate api worker web caddy
    Assert-LastExitCode "Candidate application rollout"

    Wait-ForHttps "https://localhost:8443/login"
    Wait-ForHttps "https://localhost:8443/ops/api/ready"

    $manifest | ConvertTo-Json -Depth 4 | Set-Content -Path $currentReleasePath -Encoding utf8
    Write-Host "Release $($manifest.releaseId) is ready. Previous application images remain available for rollback."
}
catch {
    if ($deploymentStarted -and (Test-Path $previousEnvironmentPath)) {
        Write-Warning "Candidate release failed readiness. Restoring previous application images without changing PostgreSQL."
        Copy-Item -Path $previousEnvironmentPath -Destination $environmentFile -Force
        $composePrevious = @("compose", "--env-file", $environmentFile, "-f", $composeFile)
        & docker @composePrevious up -d --no-build --no-deps --force-recreate api worker web caddy
        try {
            Wait-ForHttps "https://localhost:8443/login"
            Wait-ForHttps "https://localhost:8443/ops/api/ready"
        }
        catch {
            Write-Warning "Automatic application rollback also failed readiness; manual intervention is required."
        }
    }
    throw
}
finally {
    Remove-Item -Path $candidateEnvironmentPath -Force -ErrorAction SilentlyContinue
}
