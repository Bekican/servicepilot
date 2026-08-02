[CmdletBinding()]
param(
    [string]$BackupDirectory
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path $repositoryRoot "backups\staging-local"
}

if (-not (Test-Path $environmentFile)) {
    throw "Missing .env.staging.local. Start local staging before taking a backup."
}

$compose = @("compose", "--env-file", $environmentFile, "-f", $composeFile)
$postgresContainer = ([string](& docker @compose ps -q postgres)).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($postgresContainer)) {
    throw "The local staging PostgreSQL container is not running."
}

$databaseUser = ([string](& docker @compose exec -T postgres printenv POSTGRES_USER)).Trim()
$databaseName = ([string](& docker @compose exec -T postgres printenv POSTGRES_DB)).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($databaseUser) -or [string]::IsNullOrWhiteSpace($databaseName)) {
    throw "Could not resolve PostgreSQL database settings from the running container."
}

$timestamp = (Get-Date).ToUniversalTime().ToString("yyyyMMddTHHmmssZ")
$fileName = "servicepilot-staging-$timestamp.dump"
$containerDump = "/tmp/$fileName"

New-Item -ItemType Directory -Path $BackupDirectory -Force | Out-Null
$backupPath = Join-Path $BackupDirectory $fileName
$metadataPath = "$backupPath.metadata.json"
$checksumPath = "$backupPath.sha256"

$snapshotSql = @'
SELECT jsonb_build_object(
  'schemaTables', (SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'),
  'migrationTablePresent', EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '__EFMigrationsHistory'),
  'organizations', (SELECT COUNT(*) FROM organizations),
  'users', (SELECT COUNT(*) FROM users),
  'employees', (SELECT COUNT(*) FROM employees),
  'invitations', (SELECT COUNT(*) FROM user_invitations),
  'auditLogs', (SELECT COUNT(*) FROM audit_logs),
  'customers', (SELECT COUNT(*) FROM customers),
  'customerAddresses', (SELECT COUNT(*) FROM customer_addresses),
  'services', (SELECT COUNT(*) FROM services),
  'appointments', (SELECT COUNT(*) FROM appointments),
  'reminders', (SELECT COUNT(*) FROM reminders)
)::text;
'@

try {
    Write-Host "Creating a consistent PostgreSQL custom-format backup..."
    & docker @compose exec -T postgres pg_dump `
        --username $databaseUser `
        --dbname $databaseName `
        --format custom `
        --no-owner `
        --no-privileges `
        --file $containerDump
    if ($LASTEXITCODE -ne 0) {
        throw "pg_dump failed."
    }

    & docker @compose exec -T postgres pg_restore --list $containerDump | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "The generated archive could not be read by pg_restore."
    }

    & docker cp "${postgresContainer}:$containerDump" $backupPath | Out-Null
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $backupPath)) {
        throw "The backup archive could not be copied from the PostgreSQL container."
    }

    $snapshotResult = & docker @compose exec -T postgres psql `
        --username $databaseUser `
        --dbname $databaseName `
        --tuples-only `
        --no-align `
        --command $snapshotSql
    if ($LASTEXITCODE -ne 0) {
        throw "Could not capture the database verification snapshot."
    }
    $snapshotJson = ([string]$snapshotResult).Trim()

    $serverVersionResult = & docker @compose exec -T postgres psql `
        --username $databaseUser `
        --dbname $databaseName `
        --tuples-only `
        --no-align `
        --command "SHOW server_version;"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not resolve the PostgreSQL server version."
    }
    $serverVersion = ([string]$serverVersionResult).Trim()

    $gitCommit = ([string](& git -C $repositoryRoot rev-parse HEAD)).Trim()
    if ($LASTEXITCODE -ne 0) {
        $gitCommit = "unknown"
    }

    $file = Get-Item $backupPath
    $sha256 = (Get-FileHash -Path $backupPath -Algorithm SHA256).Hash.ToLowerInvariant()
    $metadata = [ordered]@{
        schemaVersion = 1
        createdAtUtc = (Get-Date).ToUniversalTime().ToString("o")
        sourceDatabase = $databaseName
        postgresVersion = $serverVersion
        gitCommit = $gitCommit
        archiveFile = $file.Name
        archiveBytes = $file.Length
        sha256 = $sha256
        snapshot = ($snapshotJson | ConvertFrom-Json)
    }

    $metadata | ConvertTo-Json -Depth 5 | Set-Content -Path $metadataPath -Encoding utf8
    "$sha256  $($file.Name)" | Set-Content -Path $checksumPath -Encoding ascii

    Write-Host "Backup created and archive structure verified."
    Write-Host "Archive:  $backupPath"
    Write-Host "Metadata: $metadataPath"
    Write-Output $backupPath
}
finally {
    & docker @compose exec -T postgres rm -f $containerDump 2>$null | Out-Null
}
