@echo off
setlocal EnableExtensions
cd /d "%~dp0"

echo ========================================
echo eNoVa_tor – Client DEBUG (Windows 11)
echo ========================================
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
  echo [.NET] SDK not found in PATH. Install .NET 10 SDK.
  pause
  exit /b 1
)

if not exist "src\Enova.Client\Enova.Client.csproj" (
  echo Run this script from the repository root.
  pause
  exit /b 1
)

echo [CFG] config\eNoVa_tor.client.properties  ^(enova.server.baseurl^)
echo [NOTE] Start the server first ^(debug-server.bat^) if you need login/API.
echo.

dotnet run --project src\Enova.Client\Enova.Client.csproj -c Debug --verbosity minimal
set EXITCODE=%ERRORLEVEL%
echo.
echo Client exited with code %EXITCODE%
pause
exit /b %EXITCODE%
