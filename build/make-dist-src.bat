setlocal

for %%P in ("%ProgramFiles%", "%ProgramFiles(x86)%") do (
  for %%S in (bin, cmd) do (
    if exist "%%~fP\Git\%%S\git.exe" (
      set GIT=%%~fP\Git\%%S\git.exe
    )
  )
)

if "%GIT%" == "" (
  echo git not found.
  pause
  exit 1
)

set PROJDIR=%~dp0..
set DISTBASE=%PROJDIR%\dist

set REPOURL=https://github.com/poderosaproject/poderosa.git

RD /S /Q "%DISTBASE%\Poderosa-X.X.X"

"%GIT%" clone --branch=master --single-branch "%REPOURL%" "%DISTBASE%\Poderosa-X.X.X"

RD /S /Q "%DISTBASE%\Poderosa-X.X.X\.git"


for /F skip^=2^ delims^=^"^ tokens^=2 %%T in ('FIND "PODEROSA_VERSION" "%DISTBASE%\Poderosa-X.X.X\Plugin\VersionInfo.cs"') do (
  set PODEROSA_VERSION=%%T
)

RD /S /Q "%DISTBASE%\Poderosa-%PODEROSA_VERSION%"
REN "%DISTBASE%\Poderosa-X.X.X" "Poderosa-%PODEROSA_VERSION%"



