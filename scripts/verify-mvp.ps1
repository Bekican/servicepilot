$ErrorActionPreference = "Stop"

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$e2eCompose = Join-Path $repositoryRoot "compose.e2e.yaml"
$webDirectory = Join-Path $repositoryRoot "apps\web"
$originalNodeEnvironment = $env:NODE_ENV
Remove-Item Env:NODE_ENV -ErrorAction SilentlyContinue

function Assert-LastExitCode([string] $step)
{
    if ($LASTEXITCODE -ne 0)
    {
        throw "$step failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForHttp([string] $uri, [int] $timeoutSeconds = 120)
{
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        try
        {
            $response = Invoke-WebRequest `
                -Uri $uri `
                -UseBasicParsing `
                -TimeoutSec 5
            if ($response.StatusCode -ge 200 `
                -and $response.StatusCode -lt 500)
            {
                return
            }
        }
        catch
        {
            Start-Sleep -Seconds 2
        }
    }

    throw "Timed out waiting for $uri."
}

Push-Location $repositoryRoot
try
{
    Write-Host "[1/8] Backend build"
    dotnet build ServicePilot.slnx
    Assert-LastExitCode "Backend build"

    Write-Host "[2/8] Backend tests"
    dotnet test ServicePilot.slnx --no-build
    Assert-LastExitCode "Backend tests"

    Write-Host "[3/8] Migration drift"
    dotnet ef migrations has-pending-model-changes `
        --project src/ServicePilot.Infrastructure `
        --startup-project src/ServicePilot.Api `
        --no-build
    Assert-LastExitCode "Migration drift check"

    Push-Location $webDirectory
    try
    {
        Write-Host "[4/8] Frontend format, lint and typecheck"
        npm run format:check
        Assert-LastExitCode "Frontend format check"
        npm run lint
        Assert-LastExitCode "Frontend lint"
        npm run typecheck
        Assert-LastExitCode "Frontend typecheck"

        Write-Host "[5/8] Frontend unit tests"
        npm test
        Assert-LastExitCode "Frontend unit tests"

        Write-Host "[6/8] Frontend production build"
        npm run build
        Assert-LastExitCode "Frontend production build"
    }
    finally
    {
        Pop-Location
    }

    Write-Host "[7/8] Isolated E2E environment"
    docker compose -f $e2eCompose down --remove-orphans
    docker compose -f $e2eCompose up --build -d
    Assert-LastExitCode "E2E environment startup"
    Wait-ForHttp "http://127.0.0.1:15267/health/ready"
    Wait-ForHttp "http://127.0.0.1:13000/login"

    Write-Host "[8/8] Playwright acceptance tests"
    Push-Location $webDirectory
    try
    {
        $env:SERVICEPILOT_WEB_URL = "http://127.0.0.1:13000"
        $env:SERVICEPILOT_E2E_API_URL = "http://127.0.0.1:15267"
        $env:SERVICEPILOT_E2E_MAILPIT_URL = "http://127.0.0.1:18025"
        $env:SERVICEPILOT_E2E_DATABASE_URL = `
            "postgresql://servicepilot_e2e:e2e-only-password@127.0.0.1:15432/servicepilot_e2e"
        npm run test:e2e
        Assert-LastExitCode "Playwright acceptance tests"
    }
    finally
    {
        Pop-Location
    }

    Write-Host "ServicePilot MVP acceptance gate passed."
}
finally
{
    docker compose -f $e2eCompose down --remove-orphans
    if ($null -eq $originalNodeEnvironment)
    {
        Remove-Item Env:NODE_ENV -ErrorAction SilentlyContinue
    }
    else
    {
        $env:NODE_ENV = $originalNodeEnvironment
    }
    Pop-Location
}
