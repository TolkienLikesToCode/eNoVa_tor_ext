#!/usr/bin/env bash
# Publish Enova.Server for Linux x64 (self-contained single file)
set -euo pipefail
if [[ ! -f src/Enova.Server/Enova.Server.csproj ]]; then
  echo "Run from repository root (folder containing src/)." >&2
  exit 1
fi

echo "=== Publish Server (linux-x64) ==="
dotnet publish src/Enova.Server/Enova.Server.csproj -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o artifacts/server-linux

mkdir -p artifacts/server-linux/config
cp -f config/eNoVa_tor.server.properties artifacts/server-linux/eNoVa_tor.server.properties 2>/dev/null || true
cp -f config/eNoVa_tor.server.properties artifacts/server-linux/config/eNoVa_tor.server.properties 2>/dev/null || true

echo "Done: artifacts/server-linux/"
echo "Note: Enova.Client is WPF and Windows-only – no Linux client publish."
