# Publish server + client (Windows x64 self-contained)
$ErrorActionPreference = "Stop"
$Root = Get-Location
if (-not (Test-Path "src\Enova.Server\Enova.Server.csproj")) {
    Write-Error "Run from repository root (folder containing src\)."
}

Write-Host "=== Publish Server (win-x64) ==="
dotnet publish src\Enova.Server\Enova.Server.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o artifacts\server
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
New-Item -ItemType Directory -Force -Path artifacts\server\config | Out-Null
Copy-Item -Force config\eNoVa_tor.server.properties artifacts\server\eNoVa_tor.server.properties -ErrorAction SilentlyContinue
Copy-Item -Force config\eNoVa_tor.server.properties artifacts\server\config\eNoVa_tor.server.properties -ErrorAction SilentlyContinue

Write-Host "=== Publish Client (win-x64) ==="
dotnet publish src\Enova.Client\Enova.Client.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o artifacts\client
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
New-Item -ItemType Directory -Force -Path artifacts\client\config | Out-Null
Copy-Item -Force config\eNoVa_tor.client.properties artifacts\client\eNoVa_tor.client.properties -ErrorAction SilentlyContinue
Copy-Item -Force config\eNoVa_tor.client.properties artifacts\client\config\eNoVa_tor.client.properties -ErrorAction SilentlyContinue

Write-Host "Done."
Write-Host "  Server: artifacts\server\"
Write-Host "  Client: artifacts\client\  (Windows only – WPF)"
