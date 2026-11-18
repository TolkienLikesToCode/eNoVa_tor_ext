#!/usr/bin/env bash
# Run Enova.Server on Linux
set -euo pipefail
if [[ ! -f src/Enova.Server/Enova.Server.csproj ]]; then
  echo "Run from repository root." >&2
  exit 1
fi

EXTERNAL=0
LISTEN=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --external) EXTERNAL=1; shift ;;
    --listen) LISTEN="$2"; shift 2 ;;
    *) echo "Usage: $0 [--external] [--listen http://0.0.0.0:5101]"; exit 1 ;;
  esac
done

if [[ "$EXTERNAL" -eq 1 ]]; then
  export ENOVA_URLS="http://0.0.0.0:5101"
elif [[ -n "$LISTEN" ]]; then
  export ENOVA_URLS="$LISTEN"
fi

export ENOVA_DB="${ENOVA_DB:-Host=localhost;Port=55432;Database=enova;Username=enova;Password=1234}"
echo "ENOVA_URLS=${ENOVA_URLS:-<from properties/default>}"
echo "ENOVA_DB=${ENOVA_DB//Password=*/Password=***}"

dotnet run --project src/Enova.Server/Enova.Server.csproj -c Release
