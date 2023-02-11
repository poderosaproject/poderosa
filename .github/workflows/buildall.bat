@echo off
setlocal

if "%1" == "" (
  echo Usage: buildall.bat config
  exit 1
)

if "%GITHUB_WORKSPACE%" == "" (
  echo missing GITHUB_WORKSPACE
  exit 1
)

call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\vsdevcmd.bat"

set CONFIGNAME=%1

set PORTFORWARDING=%GITHUB_WORKSPACE%\PortForwarding.sln
set PODEROSA=%GITHUB_WORKSPACE%\poderosa.sln
set CONTRIBPLUGINS=%GITHUB_WORKSPACE%\ContributedPlugins.sln

echo Rebuild "%PODEROSA%" "%CONFIGNAME%"
devenv.com "%PODEROSA%" /Rebuild "%CONFIGNAME%"
if ERRORLEVEL 1 goto builderr

echo Build "%PORTFORWARDING%" "%CONFIGNAME%"
devenv.com "%PORTFORWARDING%" /Build "%CONFIGNAME%"
if ERRORLEVEL 1 goto builderr

echo Build "%CONTRIBPLUGINS%" "%CONFIGNAME%"
devenv.com "%CONTRIBPLUGINS%" /Build "%CONFIGNAME%"
if ERRORLEVEL 1 goto builderr

echo Build Succeeded
exit 0

:builderr
echo Build failed !!!
exit 2
