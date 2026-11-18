@echo off
setlocal EnableExtensions
REM Run as Administrator
set SERVICE_NAME=eNoVa_tor.Server

echo Stopping and removing "%SERVICE_NAME%"...
sc stop "%SERVICE_NAME%" >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete "%SERVICE_NAME%"
echo Done.
pause
