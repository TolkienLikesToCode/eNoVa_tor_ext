# Run Enova.Server on Windows
param(
    [string]$Listen = "",
    [switch]$External
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path "src\Enova.Server\Enova.Server.csproj")) {
    Write-Error "Run from repository root."
}
if ($External) { $env:ENOVA_URLS = "http://0.0.0.0:5101" }
elseif ($Listen) { $env:ENOVA_URLS = $Listen }
Write-Host "ENOVA_URLS=$($env:ENOVA_URLS)"
if (-not $env:ENOVA_DB) {
    $env:ENOVA_DB = "Host=localhost;Port=55432;Database=enova;Username=enova;Password=1234"
}
dotnet run --project src\Enova.Server\Enova.Server.csproj -c Release
