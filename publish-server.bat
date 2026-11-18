@echo off
setlocal EnableExtensions
cd /d "%~dp0"
echo Publishing eNoVa_tor.Server (win-x64, single-file, compressed)...
dotnet publish src\Enova.Server\Enova.Server.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o artifacts\server
if errorlevel 1 goto :fail

if not exist artifacts\server\config mkdir artifacts\server\config
if exist config\eNoVa_tor.server.properties (
  copy /Y config\eNoVa_tor.server.properties artifacts\server\eNoVa_tor.server.properties >nul
  copy /Y config\eNoVa_tor.server.properties artifacts\server\config\eNoVa_tor.server.properties >nul
)
if exist config\eNoVa_tor.docuware.properties (
  copy /Y config\eNoVa_tor.docuware.properties artifacts\server\eNoVa_tor.docuware.properties >nul
)
if exist src\Enova.Server\appsettings.json (
  copy /Y src\Enova.Server\appsettings.json artifacts\server\appsettings.json >nul
)

echo.
echo Done: artifacts\server\
echo Install as Windows Service: scripts\windows\install-service.bat
echo.
pause
exit /b 0
:fail
echo Publish failed.
pause
exit /b 1
