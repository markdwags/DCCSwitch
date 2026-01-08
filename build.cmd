@echo off
setlocal enabledelayedexpansion

echo ========================================
echo Building DDCSwitch with NativeAOT
echo ========================================
echo.

REM Clean previous build
echo Cleaning previous build...
dotnet clean DDCSwitch\DDCSwitch.csproj -c Release
if errorlevel 1 (
    echo ERROR: Clean failed
    exit /b 1
)
echo.

REM Build with NativeAOT
echo Building with NativeAOT...
dotnet publish DDCSwitch\DDCSwitch.csproj -c Release -r win-x64 --self-contained
if errorlevel 1 (
    echo ERROR: Build failed
    exit /b 1
)
echo.

REM Create dist folder
echo Creating dist folder...
if not exist "dist" mkdir "dist"

REM Copy the NativeAOT executable
echo Copying executable to dist folder...
copy /Y "DDCSwitch\bin\Release\net10.0\win-x64\publish\DDCSwitch.exe" "dist\DDCSwitch.exe"
if errorlevel 1 (
    echo ERROR: Failed to copy executable
    exit /b 1
)

echo.
echo ========================================
echo Build completed successfully!
echo Output: dist\DDCSwitch.exe
echo ========================================

REM Display file size
for %%A in ("dist\DDCSwitch.exe") do (
    set size=%%~zA
    set /a sizeMB=!size! / 1048576
    echo File size: !sizeMB! MB
)

endlocal

