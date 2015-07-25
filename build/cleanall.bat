@echo off
setlocal

set PROJDIR=%~dp0..

call :clean "%PROJDIR%\Benchmark"
call :clean "%PROJDIR%\Core"
call :clean "%PROJDIR%\Executable"
call :clean "%PROJDIR%\Granados"
call :clean "%PROJDIR%\Macro"
call :clean "%PROJDIR%\Misc"
call :clean "%PROJDIR%\Monolithic"
call :clean "%PROJDIR%\Pipe"
call :clean "%PROJDIR%\Plugin"
call :clean "%PROJDIR%\PortForwarding"
call :clean "%PROJDIR%\PortForwardingCommand"
call :clean "%PROJDIR%\Protocols"
call :clean "%PROJDIR%\SerialPort"
call :clean "%PROJDIR%\SFTP"
call :clean "%PROJDIR%\TerminalEmulator"
call :clean "%PROJDIR%\TerminalSession"
call :clean "%PROJDIR%\UI"
call :clean "%PROJDIR%\Usability"
call :clean "%PROJDIR%\XZModem"

call :clean "%PROJDIR%\ContributedPlugins\ExtendPaste"

call :clean "%PROJDIR%"

pause
goto :end

:clean
echo Clean %1
if exist %1"\bin" rd /s /q %1"\bin"
if exist %1"\obj" rd /s /q %1"\obj"

:end



