$ErrorActionPreference = "Stop"

$repositoryRoot =
    Split-Path -Parent $PSScriptRoot

Push-Location $repositoryRoot
try
{
    docker compose up --build -d
    & "$PSScriptRoot\demo-seed.ps1"
}
finally
{
    Pop-Location
}

Write-Host "API: http://localhost:5267"
Write-Host "OpenAPI: http://localhost:5267/openapi/v1.json"
Write-Host "Mailpit: http://localhost:8025"
