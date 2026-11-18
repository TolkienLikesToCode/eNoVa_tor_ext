#!/usr/bin/env bash
# Start PostgreSQL for eNoVa_tor (macOS + Docker Desktop)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
COMPOSE="$ROOT/docker/docker-compose.yml"
if [[ ! -f "$COMPOSE" ]]; then
  ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
  COMPOSE="$ROOT/docker/docker-compose.yml"
fi
if [[ ! -f "$COMPOSE" ]]; then
  echo "docker/docker-compose.yml not found." >&2
  exit 1
fi

echo "=== eNoVa_tor – Database setup (macOS) ==="
if ! command -v docker >/dev/null 2>&1; then
  echo "Docker not found. Install Docker Desktop for Mac:"
  echo "  brew install --cask docker"
  echo "  or https://www.docker.com/products/docker-desktop/"
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "Start Docker Desktop from Applications, wait until it is running, then retry." >&2
  exit 1
fi

cd "$(dirname "$COMPOSE")"
docker compose up -d
echo "PostgreSQL: localhost:55432  user=enova  password=1234  db=enova"
