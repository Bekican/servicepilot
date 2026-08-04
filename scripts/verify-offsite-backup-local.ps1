[CmdletBinding()]
param(
    [switch]$SkipStagingStart,
    [switch]$RemoveMinioDataAfterSuccess
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$localComposeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$minioComposeFile = Join-Path $repositoryRoot "compose.staging.minio.yaml"
$stagingEnvironmentFile = Join-Path $repositoryRoot ".env.staging.local"
$minioEnvironmentFile = Join-Path $repositoryRoot ".env.minio.local"
$minioEnvironmentExample = Join-Path $repositoryRoot `
    "deploy/staging/.env.minio.local.example"

function Assert-LastExitCode([string]$Step) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker CLI is unavailable."
}

docker info --format '{{.ServerVersion}}' | Out-Null
Assert-LastExitCode "Docker engine readiness"

if (-not (Test-Path -LiteralPath $minioEnvironmentFile)) {
    Copy-Item -LiteralPath $minioEnvironmentExample -Destination $minioEnvironmentFile
    Write-Host "Created ignored .env.minio.local from the local drill template."
}

if (-not $SkipStagingStart) {
    & "$PSScriptRoot\start-staging-local.ps1"
}

if (-not (Test-Path -LiteralPath $stagingEnvironmentFile)) {
    throw "Missing .env.staging.local. Run start-staging-local.ps1 first."
}

$compose = @(
    "compose",
    "--env-file", $stagingEnvironmentFile,
    "--env-file", $minioEnvironmentFile,
    "-f", $localComposeFile,
    "-f", $minioComposeFile
)

Push-Location $repositoryRoot
try {
    Write-Host "[1/6] Validating the merged staging and MinIO Compose model..."
    & docker @compose config --quiet
    Assert-LastExitCode "Merged Compose validation"

    Write-Host "[2/6] Building the PostgreSQL and Restic operations image..."
    & docker @compose build backup
    Assert-LastExitCode "Backup image build"

    Write-Host "[3/6] Starting MinIO and creating the isolated drill bucket..."
    & docker @compose up -d postgres minio minio-init
    Assert-LastExitCode "MinIO startup"
    $minioInitContainer = ([string](& docker @compose ps `
        --all --quiet minio-init)).Trim()
    Assert-LastExitCode "MinIO init container lookup"
    if ([string]::IsNullOrWhiteSpace($minioInitContainer)) {
        throw "MinIO init container was not created."
    }
    $minioInitExitCode = ([string](& docker wait $minioInitContainer)).Trim()
    Assert-LastExitCode "MinIO init container wait"
    if ($minioInitExitCode -ne "0") {
        & docker @compose logs --no-color minio-init
        throw "MinIO bucket initialization failed with container exit code $minioInitExitCode."
    }

    Write-Host "[4/6] Creating, locally restoring and uploading an encrypted backup..."
    & docker @compose run --rm --no-deps backup
    Assert-LastExitCode "Encrypted MinIO backup"

    Write-Host "[5/6] Proving that the encrypted repository contains a snapshot..."
    $snapshotJson = & docker @compose run --rm --no-deps --entrypoint restic backup `
        snapshots --host servicepilot-staging-minio-drill --json
    Assert-LastExitCode "Restic snapshot listing"
    $snapshots = $snapshotJson | ConvertFrom-Json
    if ($null -eq $snapshots -or @($snapshots).Count -lt 1) {
        throw "Restic repository did not return a staging drill snapshot."
    }

    Write-Host "[6/6] Downloading and restoring the latest encrypted snapshot..."
    & docker @compose run --rm --no-deps `
        --entrypoint /usr/local/bin/servicepilot-restore-drill backup
    Assert-LastExitCode "Off-site restore drill"

    Write-Host "Local MinIO off-site backup drill passed."
    Write-Host "MinIO console: http://localhost:19001"
    Write-Host "Verified Restic snapshots: $(@($snapshots).Count)"
}
finally {
    Pop-Location
}

if ($RemoveMinioDataAfterSuccess) {
    Write-Host "Removing MinIO drill containers and data volume..."
    & docker @compose rm --stop --force minio minio-init
    Assert-LastExitCode "MinIO drill container cleanup"
    & docker volume rm servicepilot-staging-local_minio-data
    Assert-LastExitCode "MinIO drill volume cleanup"
}
else {
    Write-Host "MinIO data was kept for inspection and future drill runs."
}
