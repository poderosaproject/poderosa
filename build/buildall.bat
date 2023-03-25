@echo off
setlocal

if "%1" == "" (
  echo Usage: buildall.bat config [GA]
  exit 1
)

if "%2" == "GA" (
  if "%GITHUB_WORKSPACE%" == "" (
    echo missing GITHUB_WORKSPACE
    exit 1
  )

  echo build on GitHub Actions
  call "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\Tools\vsdevcmd.bat"

  set PRJDIR=%GITHUB_WORKSPACE%
) else (
  call "%VS120COMNTOOLS%\vsvars32.bat"
  set PRJDIR=%~dp0..
  set CLEAN=yes
)

set CONFIGNAME=%1

set PORTFORWARDING=%PRJDIR%\PortForwarding.sln
set PODEROSA=%PRJDIR%\poderosa.sln
set CONTRIBPLUGINS=%PRJDIR%\ContributedPlugins.sln

set PORTFORWARDING_LOG=%~dp0PortForwarding-build-%CONFIGNAME%.log
set PODEROSA_LOG=%~dp0Poderosa-build-%CONFIGNAME%.log
set CONTRIBPLUGINS_LOG=%~dp0ContributedPlugins-build-%CONFIGNAME%.log

if exist "%PODEROSA_LOG%"       del "%PODEROSA_LOG%"
if exist "%PORTFORWARDING_LOG%" del "%PORTFORWARDING_LOG%"
if exist "%CONTRIBPLUGINS_LOG%" del "%CONTRIBPLUGINS_LOG%"

if "%CLEAN%" == "yes" (
  devenv.exe /Clean "%CONFIGNAME%" "%PODEROSA%"
  if ERRORLEVEL 1 goto builderr

  devenv.exe /Clean "%CONFIGNAME%" "%PORTFORWARDING%"
  if ERRORLEVEL 1 goto builderr

  devenv.exe /Clean "%CONFIGNAME%" "%CONTRIBPLUGINS%"
  if ERRORLEVEL 1 goto builderr
)

devenv.exe /Rebuild "%CONFIGNAME%" "%PODEROSA%"       /Out "%PODEROSA_LOG%"
if ERRORLEVEL 1 goto builderr

devenv.exe /Build   "%CONFIGNAME%" "%PORTFORWARDING%" /Out "%PORTFORWARDING_LOG%"
if ERRORLEVEL 1 goto builderr

devenv.exe /Build   "%CONFIGNAME%" "%CONTRIBPLUGINS%" /Out "%CONTRIBPLUGINS_LOG%"
if ERRORLEVEL 1 goto builderr

echo Build Succeeded
exit 0

:builderr
echo Build failed !!!
exit 2
