@echo off
echo Publishing KaiserVox as single-file executable...

REM Use full path to dotnet if not in PATH
set DOTNET="C:\Program Files\dotnet\dotnet.exe"
if not exist %DOTNET% set DOTNET=dotnet

REM Publish
echo.
echo Publishing...
%DOTNET% publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o publish
if %ERRORLEVEL% neq 0 (
    echo Error: Publish failed
    exit /b 1
)

echo.
echo Publish successful!
echo Output: publish\KaiserVox.exe
echo.
echo You can now distribute publish\KaiserVox.exe

