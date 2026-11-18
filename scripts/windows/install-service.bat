@echo off
setlocal EnableExtensions
REM Run as Administrator
cd /d "%~dp0..\.."

set SERVICE_NAME=eNoVa_tor.Server
set DISPLAY_NAME=eNoVa_tor Server
set BIN=%CD%\artifacts\server\eNoVa_tor.Server.exe

if not exist "%BIN%" (
  echo Published server not found: %BIN%
  echo Run publish-server.bat first.
  pause
  exit /b 1
)

echo Installing Windows Service "%SERVICE_NAME%"
echo Binary: %BIN%
echo.

sc query "%SERVICE_NAME%" >nul 2>&1
if not errorlevel 1 (
  echo Service already exists – stopping and deleting...
  sc stop "%SERVICE_NAME%" >nul 2>&1
  timeout /t 2 /nobreak >nul
  sc delete "%SERVICE_NAME%"
  timeout /t 2 /nobreak >nul
)

sc create "%SERVICE_NAME%" binPath= "\"%BIN%\"" start= auto DisplayName= "%DISPLAY_NAME%"
if errorlevel 1 (
  echo sc create failed. Run this script as Administrator.
  pause
  exit /b 1
)

sc description "%SERVICE_NAME%" "eNoVa_tor API (ASP.NET Core). Config next to EXE / environment variables."
sc failure "%SERVICE_NAME%" reset= 86400 actions= restart/5000/restart/10000/restart/30000

echo Starting service...
sc start "%SERVICE_NAME%"
echo.
echo Done. Configure network via artifacts\server\eNoVa_tor.server.properties or ENOVA_* env vars on the service account.
pause
