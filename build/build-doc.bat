set MSBUILD=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe

cd "%~dp0.."

"%MSBUILD%" /p:Configuration=Release "%~dp0..\Doc\PoderosaAPI_ja.shfbproj"
"%MSBUILD%" /p:Configuration=Release "%~dp0..\Doc\PoderosaAPI_en.shfbproj"
"%MSBUILD%" /p:Configuration=Release "%~dp0..\Doc\PoderosaMacroAPI_ja.shfbproj"
"%MSBUILD%" /p:Configuration=Release "%~dp0..\Doc\PoderosaMacroAPI_en.shfbproj"

pause
