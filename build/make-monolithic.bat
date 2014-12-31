@echo off
setlocal

set ILMERGE=%ProgramFiles%\Microsoft\ILMerge\ILMerge.exe

if not exist "%ILMERGE%" (
  echo ILMerge.exe is reuired to make a Monolithic-Poderosa.
  pause
  goto end
)

set CONFIG=Release
set PROJDIR=%~dp0..

set ASSYS=

call :addfile "%PROJDIR%\Executable\bin\%CONFIG%\Poderosa.exe"

for %%D in (Core Granados Macro Pipe Plugin PortForwardingCommand Protocols SerialPort SFTP TerminalEmulator TerminalSession UI Usability XZModem Benchmark) do (
  if exist "%PROJDIR%\%%D\bin\%CONFIG%\%%D.dll" (
    call :addfile "%PROJDIR%\%%D\bin\%CONFIG%\%%D.dll"
  ) else if exist "%PROJDIR%\%%D\bin\%CONFIG%\Poderosa.%%D.dll" (
    call :addfile "%PROJDIR%\%%D\bin\%CONFIG%\Poderosa.%%D.dll"
  )
)

"%ILMERGE%" /targetplatform:v2 /target:winexe /copyattrs /allowMultiple /out:poderosa.monolithic.exe %ASSYS%
pause

goto end

:addfile
set ASSYS=%ASSYS% %1
goto end

:end
