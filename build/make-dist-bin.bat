setlocal

set PROJDIR=%~dp0..
set DISTBASE=%PROJDIR%\dist
set DIST=%DISTBASE%\Poderosa
set DISTCONTRIB=%DISTBASE%\ContributedPlugins
set DOCDIST_EN=%DISTBASE%\EN\Documents
set DOCDIST_JA=%DISTBASE%\JA\Documents
set BINDIR=%PROJDIR%\bin\Release

RD /S /Q "%DISTBASE%"
MD "%DISTBASE%"
MD "%DIST%"
MD "%DISTCONTRIB%"
MD "%DOCDIST_EN%"
MD "%DOCDIST_JA%"

copy "%BINDIR%\Poderosa.exe" "%DIST%"
copy "%BINDIR%\Poderosa.pdb" "%DIST%"

copy "%BINDIR%\Poderosa.Plugin.dll" "%DIST%"
copy "%BINDIR%\Poderosa.Plugin.pdb" "%DIST%"

copy "%BINDIR%\Granados.dll" "%DIST%"
copy "%BINDIR%\Granados.pdb" "%DIST%"

for %%P in (Core Macro PortForwardingCommand Protocols SerialPort TerminalEmulator TerminalSession UI Usability XZModem Pipe SFTP) do (
  MD "%DIST%\%%P"
  copy "%BINDIR%\Poderosa.%%P.dll" "%DIST%\%%P"
  copy "%BINDIR%\Poderosa.%%P.pdb" "%DIST%\%%P"
)

copy "%BINDIR%\charwidth" "%DIST%\Core"
copy "%BINDIR%\charfont" "%DIST%\Core"

MD "%DIST%\CygwinBridge"
copy "%BINDIR%\CygwinBridge\*" "%DIST%\CygwinBridge"

MD "%DIST%\Macro\Sample"
copy "%PROJDIR%\Misc\Macro\Sample\*.js" "%DIST%\Macro\Sample"

MD "%DIST%\Portforwarding"
copy "%BINDIR%\Portforwarding.exe" "%DIST%\Portforwarding"
copy "%BINDIR%\Portforwarding.pdb" "%DIST%\Portforwarding"
copy "%BINDIR%\Granados.dll" "%DIST%\Portforwarding"
copy "%BINDIR%\Granados.pdb" "%DIST%\Portforwarding"

copy "%PROJDIR%\LICENSE.txt" "%DIST%"
copy "%PROJDIR%\ChangeLog.txt" "%DIST%"
copy "%PROJDIR%\ABOUT ENCODINGS.txt" "%DIST%"

: MD "%DIST%\Monolithic"
: copy "%PROJDIR%\Misc\Scripts\Monolithic\*" "%DIST%\Monolithic"

REM =====================================

copy "%PROJDIR%\Doc\Help\*_en.chm" "%DOCDIST_EN%"
copy "%PROJDIR%\Doc\Help\*_ja.chm" "%DOCDIST_JA%"

REM =====================================

for %%P in ( ExtendPaste ) do (
  MD "%DISTCONTRIB%\%%P"
  copy "%BINDIR%\%%P\*.dll" "%DISTCONTRIB%\%%P"
  copy "%BINDIR%\%%P\*.pdb" "%DISTCONTRIB%\%%P"
  if exist "%BINDIR%\%%P\README*.txt" (
    copy "%BINDIR%\%%P\README*.txt" "%DISTCONTRIB%\%%P"
  )
)
