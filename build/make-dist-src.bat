@echo off
setlocal

set PROJDIR=%~dp0..
set DISTBASE=%PROJDIR%\dist

set GIT=C:\Program Files\Git\bin\git.exe
set REPOURL=https://github.com/poderosaproject/poderosa.git

RD /S /Q "%DISTBASE%\Poderosa-X.X.X"

"%GIT%" clone "%REPOURL%" "%DISTBASE%\Poderosa-X.X.X"

RD /S /Q "%DISTBASE%\Poderosa-X.X.X\.git"

pause




