setlocal

set PROJDIR=%~dp0..
set DISTBASE=%PROJDIR%\dist
set DIST=%DISTBASE%\Poderosa
set DISTCONTRIB=%DISTBASE%\ContributedPlugins
set DOCDIST_EN=%DISTBASE%\EN\Documents
set DOCDIST_JA=%DISTBASE%\JA\Documents
set BINDIR=\..\bin\Release

RD /S /Q "%DISTBASE%"
MD "%DISTBASE%"
MD "%DIST%"
MD "%DISTCONTRIB%"
MD "%DOCDIST_EN%"
MD "%DOCDIST_JA%"

copy "%PROJDIR%\Executable%BINDIR%\Poderosa.exe" "%DIST%"
copy "%PROJDIR%\Executable%BINDIR%\Poderosa.pdb" "%DIST%"

copy "%PROJDIR%\Plugin%BINDIR%\Poderosa.Plugin.dll" "%DIST%"
copy "%PROJDIR%\Plugin%BINDIR%\Poderosa.Plugin.pdb" "%DIST%"

copy "%PROJDIR%\Granados%BINDIR%\Granados.dll" "%DIST%"
copy "%PROJDIR%\Granados%BINDIR%\Granados.pdb" "%DIST%"

for %%P in (Core Macro PortForwardingCommand Protocols SerialPort TerminalEmulator TerminalSession UI Usability XZModem Pipe SFTP) do (
  MD "%DIST%\%%P"
  copy "%PROJDIR%\%%P%BINDIR%\Poderosa.%%P.dll" "%DIST%\%%P"
  copy "%PROJDIR%\%%P%BINDIR%\Poderosa.%%P.pdb" "%DIST%\%%P"
)

MD "%DIST%\Protocols\Cygterm"
copy "%PROJDIR%\Misc\CygTerm\*.*" "%DIST%\Protocols\Cygterm"

MD "%DIST%\Macro\Sample"
copy "%PROJDIR%\Misc\Macro\Sample\*.js" "%DIST%\Macro\Sample"

MD "%DIST%\Portforwarding"
copy "%PROJDIR%\Portforwarding%BINDIR%\Portforwarding.exe" "%DIST%\Portforwarding"
copy "%PROJDIR%\Portforwarding%BINDIR%\Portforwarding.pdb" "%DIST%\Portforwarding"
copy "%PROJDIR%\Granados%BINDIR%\Granados.dll" "%DIST%\Portforwarding"
copy "%PROJDIR%\Granados%BINDIR%\Granados.pdb" "%DIST%\Portforwarding"

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
  copy "%PROJDIR%\%%P%BINDIR%\Poderosa.%%P.dll" "%DISTCONTRIB%\%%P"
  copy "%PROJDIR%\%%P%BINDIR%\Poderosa.%%P.pdb" "%DISTCONTRIB%\%%P"

  if exist "%PROJDIR%\ContributedPlugins\%%P\README*.txt" (
    copy "%PROJDIR%\ContributedPlugins\%%P\README*.txt" "%DISTCONTRIB%\%%P"
  )
)

pause
