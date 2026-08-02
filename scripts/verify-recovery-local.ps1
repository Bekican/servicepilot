[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "Running PostgreSQL backup and isolated restore drill..."
$backupOutput = & "$PSScriptRoot\backup-staging-local.ps1"
$backupPath = @($backupOutput | Where-Object { $_ -is [string] -and $_.EndsWith(".dump") }) | Select-Object -Last 1

if ([string]::IsNullOrWhiteSpace($backupPath)) {
    throw "The backup script did not return an archive path."
}

& "$PSScriptRoot\restore-staging-local.ps1" -BackupPath $backupPath
if ($LASTEXITCODE -ne 0) {
    throw "The PostgreSQL recovery drill failed."
}

Write-Host "PostgreSQL recovery verification passed."
