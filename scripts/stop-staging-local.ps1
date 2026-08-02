$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot "compose.staging.local.yaml"
$environmentFile = Join-Path $repositoryRoot ".env.staging.local"

Push-Location $repositoryRoot
try
{
    docker compose `
        --env-file $environmentFile `
        -f $composeFile `
        down --remove-orphans
}
finally
{
    Pop-Location
}

Write-Host `
    "Local staging stopped. PostgreSQL and Caddy volumes were preserved."
