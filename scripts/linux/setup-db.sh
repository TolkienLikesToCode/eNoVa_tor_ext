#!/usr/bin/env bash
# Start PostgreSQL for eNoVa_tor (Linux + Docker Engine/Compose)
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

echo "=== eNoVa_tor – Database setup (Linux) ==="
if ! command -v docker >/dev/null 2>&1; then
  echo "Docker not found. Install Docker Engine + Compose plugin, e.g.:"
  echo "  https://docs.docker.com/engine/install/"
  exit 1
fi
if ! docker info >/dev/null 2>&1; then
  echo "Docker is not running or current user lacks permission (try: sudo usermod -aG docker \$USER)." >&2
  exit 1
fi

cd "$(dirname "$COMPOSE")"
docker compose up -d
echo "PostgreSQL: localhost:55432  user=enova  password=1234  db=enova"
