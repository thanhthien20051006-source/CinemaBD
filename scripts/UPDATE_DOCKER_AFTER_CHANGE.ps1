param(
    [switch]$NoBuildCheck
)

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDir
Set-Location $root

if (-not $NoBuildCheck) {
    dotnet build "$root\CinemaBD.WebApi.sln" /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }
}

docker info | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Docker Desktop is not running. Open Docker Desktop and run this script again." }

docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw "docker compose up failed" }

Write-Host ""
Write-Host "Docker status:" -ForegroundColor Cyan
docker compose ps

Write-Host ""
Write-Host "Quick health check:" -ForegroundColor Cyan
try {
    $api = (Invoke-WebRequest -UseBasicParsing http://localhost:5188/api/movies -TimeoutSec 30).StatusCode
    Write-Host "API  http://localhost:5188/api/movies => $api" -ForegroundColor Green
} catch {
    Write-Host "API check failed: $($_.Exception.Message)" -ForegroundColor Red
}

try {
    $web = (Invoke-WebRequest -UseBasicParsing http://localhost:7188/ -TimeoutSec 30).StatusCode
    Write-Host "WEB  http://localhost:7188/ => $web" -ForegroundColor Green
} catch {
    Write-Host "WEB check failed: $($_.Exception.Message)" -ForegroundColor Red
}
