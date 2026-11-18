@echo off
setlocal EnableExtensions
cd /d "%~dp0"
echo Publishing eNoVa_tor.Client (win-x64, single-file, compressed, with icon)...
dotnet publish src\Enova.Client\Enova.Client.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:ApplicationIcon=Resources\icon.ico ^
  -o artifacts\client
if errorlevel 1 goto :fail

if not exist artifacts\client\config mkdir artifacts\client\config
if exist config\eNoVa_tor.client.properties (
  copy /Y config\eNoVa_tor.client.properties artifacts\client\eNoVa_tor.client.properties >nul
  copy /Y config\eNoVa_tor.client.properties artifacts\client\config\eNoVa_tor.client.properties >nul
)

echo.
echo Done: artifacts\client\
echo.
pause
exit /b 0
:fail
echo Publish failed.
pause
exit /b 1
