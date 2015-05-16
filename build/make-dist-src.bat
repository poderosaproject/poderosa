@echo off
setlocal

set PROJDIR=%~dp0..
set DISTBASE=%PROJDIR%\dist

set GIT=C:\Program Files\Git\bin\git.exe
set REPOURL=https://github.com/poderosaproject/poderosa.git

RD /S /Q "%DISTBASE%\Poderosa-X.X.X"

"%GIT%" clone "%REPOURL%" "%DISTBASE%\Poderosa-X.X.X"

RD /S /Q "%DISTBASE%\Poderosa-X.X.X\.git"


for /F skip^=2^ delims^=^"^ tokens^=2 %%T in ('FIND "PODEROSA_VERSION" "%DISTBASE%\Poderosa-X.X.X\Plugin\VersionInfo.cs"') do (
  set PODEROSA_VERSION=%%T
)

RD /S /Q "%DISTBASE%\Poderosa-%PODEROSA_VERSION%"
REN "%DISTBASE%\Poderosa-X.X.X" "Poderosa-%PODEROSA_VERSION%"

pause




