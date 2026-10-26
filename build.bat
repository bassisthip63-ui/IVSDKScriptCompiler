@echo off
rem Builds IVSDKScriptCompiler.ivsdk.dll with the C# compiler that comes with Windows (.NET Framework 4).
rem Put this folder inside your GTA IV folder (next to GTAIV.exe), or set GTAIV below.
setlocal
set "HERE=%~dp0"
set "GTAIV="
if exist "%HERE%..\IVSDKDotNetWrapper.dll" set "GTAIV=%HERE%.."
if exist "%HERE%IVSDKDotNetWrapper.dll" set "GTAIV=%HERE%."
if "%GTAIV%"=="" (
    echo Could not find IVSDKDotNetWrapper.dll.
    echo Put this folder inside your GTA IV folder, next to GTAIV.exe, and run build.bat again.
    pause & exit /b 1
)

set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
    echo Could not find the .NET Framework 4 C# compiler at:
    echo   %CSC%
    pause & exit /b 2
)

set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
"%CSC%" /nologo /target:library /platform:x86 /optimize+ ^
    /out:"%HERE%IVSDKScriptCompiler.ivsdk.dll" ^
    /reference:"%GTAIV%\IVSDKDotNetWrapper.dll" ^
    /reference:"%FW%\System.dll" ^
    "%HERE%IVSDKScriptCompiler.cs"
if errorlevel 1 (
    echo.
    echo BUILD FAILED - see the messages above. Report them at https://github.com/bassisthip63-ui/IVSDKScriptCompiler/issues
    pause & exit /b 3
)

if not exist "%GTAIV%\IVSDKDotNet\scripts" mkdir "%GTAIV%\IVSDKDotNet\scripts"
copy /y "%HERE%IVSDKScriptCompiler.ivsdk.dll" "%GTAIV%\IVSDKDotNet\scripts\" >nul
echo.
echo Built and installed: %GTAIV%\IVSDKDotNet\scripts\IVSDKScriptCompiler.ivsdk.dll
pause
