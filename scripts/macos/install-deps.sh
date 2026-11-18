#!/usr/bin/env bash
# Install dependencies on macOS (Homebrew)
set -euo pipefail
echo "=== eNoVa_tor – dependency helper (macOS) ==="

if ! command -v brew >/dev/null 2>&1; then
  echo "Install Homebrew: https://brew.sh"
  exit 1
fi

# .NET 10 SDK
if ! command -v dotnet >/dev/null 2>&1; then
  echo "Installing .NET (via brew)…"
  brew install dotnet || brew install --cask dotnet-sdk
else
  echo "dotnet found: $(dotnet --version)"
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Installing Docker Desktop…"
  brew install --cask docker
  echo "Open Docker.app once and finish the setup wizard."
else
  echo "docker found"
fi

echo "Optional: brew install caddy  # or nginx"
echo "Done."
