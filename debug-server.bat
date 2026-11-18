@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ========================================
echo eNoVa_tor – Server DEBUG (Windows 11)
echo ========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [.NET] SDK not found in PATH. Install .NET 10 SDK.
  pause
  exit /b 1
)

if not exist "src\Enova.Server\Enova.Server.csproj" (
  echo Run this script from the repository root.
  pause
  exit /b 1
)

REM Optional overrides - uncomment as needed:
REM set ENOVA_URLS=http://127.0.0.1:5101
REM set ENOVA_NETWORK_MODE=local
REM set ENOVA_SIGNING_ENABLED=false
REM set ENOVA_DB=Host=localhost;Port=55432;Database=enova;Username=enova;Password=1234

echo [DB] Ensure PostgreSQL is up: start-db.bat or docker compose -f docker\docker-compose.yml up -d
echo [CFG] config\eNoVa_tor.server.properties
echo.

dotnet run --project src\Enova.Server\Enova.Server.csproj -c Debug --verbosity minimal
set EXITCODE=%ERRORLEVEL%
echo.
echo Server exited with code %EXITCODE%
pause
exit /b %EXITCODE%
