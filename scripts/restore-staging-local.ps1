[CmdletBinding()]
param(
    [string]$BackupPath,
    [string]$BackupDirectory,
    [switch]$KeepVerificationDatabase
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path $repositoryRoot "backups\staging-local"
}

if ([string]::IsNullOrWhiteSpace($BackupPath)) {
    $latestBackup = Get-ChildItem -Path $BackupDirectory -Filter "*.dump" -File -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
    if ($null -eq $latestBackup) {
        throw "No staging backup was found in $BackupDirectory."
    }
    $BackupPath = $latestBackup.FullName
}

$BackupPath = [System.IO.Path]::GetFullPath($BackupPath)
$metadataPath = "$BackupPath.metadata.json"
if (-not (Test-Path $BackupPath) -or -not (Test-Path $metadataPath)) {
    throw "The backup archive and its metadata file must both exist."
}

if (-not (Test-Path $environmentFile)) {
    throw "Missing .env.staging.local. Start local staging before running a restore drill."
}

$metadata = Get-Content -Path $metadataPath -Raw | ConvertFrom-Json
$actualSha256 = (Get-FileHash -Path $BackupPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualSha256 -ne $metadata.sha256) {
    throw "Backup checksum mismatch. Restore was stopped before touching PostgreSQL."
}

$compose = @("compose", "--env-file", $environmentFile, "-f", $composeFile)
$postgresContainer = ([string](& docker @compose ps -q postgres)).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($postgresContainer)) {
    throw "The local staging PostgreSQL container is not running."
}

$databaseUser = ([string](& docker @compose exec -T postgres printenv POSTGRES_USER)).Trim()
$sourceDatabase = ([string](& docker @compose exec -T postgres printenv POSTGRES_DB)).Trim()
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 8)
$verificationDatabase = "servicepilot_restore_verify_$((Get-Date).ToUniversalTime().ToString('yyyyMMddHHmmss'))_$suffix"

if ($verificationDatabase -notmatch '^servicepilot_restore_verify_[a-z0-9_]+$') {
    throw "Generated verification database name failed the safety check."
}
if ($verificationDatabase -eq $sourceDatabase) {
    throw "Restore drills must never target the active staging database."
}

$archiveName = [System.IO.Path]::GetFileName($BackupPath)
$containerDump = "/tmp/$archiveName"
$databaseCreated = $false

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
    Write-Host "Checksum verified. Restoring into isolated database $verificationDatabase..."
    & docker cp $BackupPath "${postgresContainer}:$containerDump" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not copy the backup into the PostgreSQL container."
    }

    & docker @compose exec -T postgres pg_restore --list $containerDump | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "pg_restore could not read the backup archive."
    }

    & docker @compose exec -T postgres createdb `
        --username $databaseUser `
        --template template0 `
        $verificationDatabase
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create the isolated verification database."
    }
    $databaseCreated = $true

    & docker @compose exec -T postgres pg_restore `
        --exit-on-error `
        --no-owner `
        --no-privileges `
        --username $databaseUser `
        --dbname $verificationDatabase `
        $containerDump
    if ($LASTEXITCODE -ne 0) {
        throw "The restore operation failed."
    }

    & docker @compose exec -T postgres psql `
        --username $databaseUser `
        --dbname $verificationDatabase `
        --command "ANALYZE;" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Post-restore ANALYZE failed."
    }

    $restoredSnapshotResult = & docker @compose exec -T postgres psql `
        --username $databaseUser `
        --dbname $verificationDatabase `
        --tuples-only `
        --no-align `
        --command $snapshotSql
    if ($LASTEXITCODE -ne 0) {
        throw "Could not verify the restored database."
    }
    $restoredSnapshotJson = ([string]$restoredSnapshotResult).Trim()

    $expectedSnapshot = $metadata.snapshot | ConvertTo-Json -Compress
    $restoredSnapshot = ($restoredSnapshotJson | ConvertFrom-Json) | ConvertTo-Json -Compress
    if ($expectedSnapshot -ne $restoredSnapshot) {
        throw "Restore completed, but verification counts do not match the backup metadata."
    }

    Write-Host "Restore drill passed: schema footprint and business-table counts match the backup."
    if ($KeepVerificationDatabase) {
        Write-Host "Verification database kept: $verificationDatabase"
    }
}
finally {
    & docker @compose exec -T postgres rm -f $containerDump 2>$null | Out-Null

    if ($databaseCreated -and -not $KeepVerificationDatabase) {
        if ($verificationDatabase -notmatch '^servicepilot_restore_verify_[a-z0-9_]+$') {
            throw "Refusing to drop a database that failed the verification-name safety check."
        }
        & docker @compose exec -T postgres dropdb `
            --username $databaseUser `
            --if-exists `
            --force `
            $verificationDatabase | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "The isolated verification database could not be removed: $verificationDatabase"
        }
    }
}
