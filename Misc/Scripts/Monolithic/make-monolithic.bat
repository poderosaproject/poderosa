@echo off
rem *********************************************************
rem  Makes Monolithic Poderosa
rem 
rem  Note:
rem    ILMerge is required to make a monolithic poderosa.
rem    You can download it from Microsoft.
rem *********************************************************
setlocal

rem =====!!! CHANGE THIS !!!=================================

set ILMERGE=C:\Program Files (x86)\Microsoft\ILMerge\ILMerge.exe

set TARGET=Poderosa-monolithic.exe

rem =========================================================

if exist "%ILMERGE%" (
  goto :SCAN
)
if exist "%ProgramFiles%\Microsoft\ILMerge\ILMerge.exe" (
  set ILMERGE=%ProgramFiles%\Microsoft\ILMerge\ILMerge.exe
  goto :SCAN
)
if exist "%ProgramFiles(x86)%\Microsoft\ILMerge\ILMerge.exe" (
  set ILMERGE=%ProgramFiles(x86)%\Microsoft\ILMerge\ILMerge.exe
  goto :SCAN
)

echo ILMerge not found.
echo Edit this batch file and set correct path to ILMERGE.
pause
goto :END

rem ---------------------------------------------------------
:SCAN
echo ILMerge: %ILMERGE%
echo.

set ASSYS=
pushd %~dp0

for %%F in (Poderosa.exe Granados.dll Poderosa.Plugin.dll) do (
  if not exist "..\%%F" (
    echo ..\%%F not found.
    pause
    goto :END
  )
  call :ADDFILE "..\%%F"
)

for %%D in (Core Macro Pipe PortForwardingCommand Protocols SerialPort SFTP TerminalEmulator TerminalSession UI Usability XZModem) do (
  if exist "..\%%D\%%D.dll" (
    call :ADDFILE "..\%%D\%%D.dll"
  ) else if exist "..\%%D\Poderosa.%%D.dll" (
    call :ADDFILE "..\%%D\Poderosa.%%D.dll"
  )
)

rem ---------------------------------------------------------
:MERGE
echo.
echo These files will be merged to : %TARGET%

if exist "%TARGET%" (
  echo %TARGET% already exists. It will be overwritten.
)

echo.
pause

echo Merging...

"%ILMERGE%" /targetplatform:v2 /target:winexe /copyattrs /allowMultiple /out:%TARGET% %ASSYS%

echo Done.

pause
popd
goto :END

rem ---------------------------------------------------------
:ADDFILE
set ASSYS=%ASSYS% %1
echo File: %~1
goto :END

rem ---------------------------------------------------------
:END
