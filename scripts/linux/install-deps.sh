#!/usr/bin/env bash
# Install build/runtime dependencies on common Linux distros (Debian/Ubuntu/RHEL-ish)
set -euo pipefail
echo "=== eNoVa_tor – dependency helper (Linux) ==="

if command -v apt-get >/dev/null 2>&1; then
  sudo apt-get update
  # .NET 10 – official Microsoft package feed recommended:
  # https://learn.microsoft.com/dotnet/core/install/linux
  echo "Install .NET 10 SDK manually from Microsoft docs if not present."
  echo "  wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh"
  echo "  bash /tmp/dotnet-install.sh --channel 10.0"
  if ! command -v docker >/dev/null 2>&1; then
    echo "Install Docker: https://docs.docker.com/engine/install/ubuntu/"
  fi
elif command -v dnf >/dev/null 2>&1; then
  echo "Use Microsoft packages for .NET 10 and Docker docs for your distro."
else
  echo "Unknown package manager. Install .NET 10 SDK and Docker manually."
fi

echo "Optional TLS reverse-proxy: caddy or nginx"
echo "  Debian: sudo apt-get install -y nginx"
