@echo off
echo Building KaiserVox...

REM Check for dotnet
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo Error: .NET SDK not found. Please install from https://dotnet.microsoft.com/download
    exit /b 1
)

REM Restore packages
echo.
echo Restoring NuGet packages...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo Error: Failed to restore packages
    exit /b 1
)

REM Build
echo.
echo Building...
dotnet build -c Release
if %ERRORLEVEL% neq 0 (
    echo Error: Build failed
    exit /b 1
)

echo.
echo Build successful!
echo Output: bin\Release\net8.0-windows\

