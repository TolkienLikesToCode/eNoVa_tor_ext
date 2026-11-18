#!/usr/bin/env bash
# Publish Enova.Server for macOS (arm64 Apple Silicon and/or x64)
set -euo pipefail
if [[ ! -f src/Enova.Server/Enova.Server.csproj ]]; then
  echo "Run from repository root (folder containing src/)." >&2
  exit 1
fi

ARCH="$(uname -m)"
if [[ "$ARCH" == "arm64" ]]; then
  RID="osx-arm64"
else
  RID="osx-x64"
fi

echo "=== Publish Server ($RID) ==="
dotnet publish src/Enova.Server/Enova.Server.csproj -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o "artifacts/server-$RID"

mkdir -p "artifacts/server-$RID/config"
cp -f config/eNoVa_tor.server.properties "artifacts/server-$RID/eNoVa_tor.server.properties" 2>/dev/null || true
cp -f config/eNoVa_tor.server.properties "artifacts/server-$RID/config/eNoVa_tor.server.properties" 2>/dev/null || true

echo "Done: artifacts/server-$RID/"
echo "Note: Enova.Client is WPF and Windows-only – no macOS client."
