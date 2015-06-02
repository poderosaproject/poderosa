@echo off
setlocal

if "%1" == "" (
  echo Usage: buildall.bat config
  goto :end
)

call "%VS120COMNTOOLS%\vsvars32.bat"

set CONFIGNAME=%1
set PORTFORWARDING=%~dp0..\PortForwarding.sln
set PODEROSA=%~dp0..\poderosa.sln
set PORTFORWARDING_LOG=%~dp0PortForwarding-build-%CONFIGNAME%.log
set PODEROSA_LOG=%~dp0Poderosa-build-%CONFIGNAME%.log

if exist "%PODEROSA_LOG%"       del "%PODEROSA_LOG%"
if exist "%PORTFORWARDING_LOG%" del "%PORTFORWARDING_LOG%"

devenv.exe /Clean "%CONFIGNAME%" "%PODEROSA%"
if ERRORLEVEL 1 goto builderr

devenv.exe /Clean "%CONFIGNAME%" "%PORTFORWARDING%"
if ERRORLEVEL 1 goto builderr

devenv.exe /Rebuild "%CONFIGNAME%" "%PODEROSA%"       /Out "%PODEROSA_LOG%"
if ERRORLEVEL 1 goto builderr

devenv.exe /Build   "%CONFIGNAME%" "%PORTFORWARDING%" /Out "%PORTFORWARDING_LOG%"
if ERRORLEVEL 1 goto builderr

echo Build Succeeded
goto end

:builderr
echo Build failed !!!

:end
pause
