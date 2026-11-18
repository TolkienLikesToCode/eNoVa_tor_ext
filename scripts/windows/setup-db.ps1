# Start PostgreSQL for eNoVa_tor (Windows 11 + Docker Desktop)
$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
if (-not (Test-Path (Join-Path $Root "docker\docker-compose.yml"))) {
    $Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
}
$Compose = Join-Path $Root "docker\docker-compose.yml"
if (-not (Test-Path $Compose)) {
    Write-Error "docker/docker-compose.yml not found. Run from the repo root layout."
}

Write-Host "=== eNoVa_tor – Database setup (Windows) ==="
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Docker not found. Install Docker Desktop:"
    Write-Host "  winget install -e --id Docker.DockerDesktop"
    exit 1
}
docker info 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Docker is installed but not running. Start Docker Desktop and retry."
}

Set-Location (Split-Path $Compose)
docker compose up -d
Write-Host "PostgreSQL: localhost:55432  user=enova  password=1234  db=enova"
