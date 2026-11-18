# Start Enova.Server reachable on the LAN (plain HTTP – use only behind VPN / reverse proxy)
# Prefer: bind Kestrel to localhost and put Caddy/nginx with TLS in front (see ARBEITSHILFE.md)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root "src\Enova.Server"))) {
    $root = Get-Location
}

# Bind all interfaces; clients still need enova.server.baseurl pointing here
$env:ENOVA_URLS = "http://0.0.0.0:5101"

Write-Host "Starting Enova.Server on $env:ENOVA_URLS ..."
Set-Location (Join-Path $root "src\Enova.Server")
dotnet run -c Release
