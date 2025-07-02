@echo off
echo ====================================================
echo FlowBox Unity Package Fix Script
echo ====================================================
echo.

echo Step 1: Cleaning Unity Package Cache...
echo.

REM Close Unity if running
taskkill /f /im Unity.exe 2>nul
taskkill /f /im UnityHub.exe 2>nul

echo Closed Unity processes
echo.

REM Clean Unity cache directories
echo Cleaning Unity cache directories...
if exist "%LOCALAPPDATA%\Unity\cache" (
    echo Removing %LOCALAPPDATA%\Unity\cache
    rmdir /s /q "%LOCALAPPDATA%\Unity\cache" 2>nul
)

if exist "%APPDATA%\Unity\Asset Store-5.x" (
    echo Removing %APPDATA%\Unity\Asset Store-5.x
    rmdir /s /q "%APPDATA%\Unity\Asset Store-5.x" 2>nul
)

REM Clean project-specific cache
echo Cleaning project cache...
if exist "Library" (
    echo Removing Library folder...
    rmdir /s /q "Library" 2>nul
)

if exist "Temp" (
    echo Removing Temp folder...
    rmdir /s /q "Temp" 2>nul
)

if exist "obj" (
    echo Removing obj folder...
    rmdir /s /q "obj" 2>nul
)

if exist "Logs" (
    echo Removing Logs folder...
    rmdir /s /q "Logs" 2>nul
)

echo.
echo Step 2: Setting proper permissions...
echo.

REM Set permissions for current directory
icacls . /grant:r "%USERNAME%":(OI)(CI)F /T /Q 2>nul
icacls Packages /grant:r "%USERNAME%":(OI)(CI)F /T /Q 2>nul

echo.
echo Step 3: Package cleanup complete!
echo.
echo NEXT STEPS:
echo 1. Open Unity Hub
echo 2. Open this project (FlowBox)
echo 3. Let Unity reimport all packages
echo 4. Wait for compilation to complete
echo.
echo If you still get errors, try running Unity as Administrator
echo.
pause 