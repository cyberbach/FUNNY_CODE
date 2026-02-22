@echo off
REM ============================================================
REM Batch-файл для сборки shipping-версии .NET проекта
REM ============================================================

setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "CSPROJ_FILE="

for %%f in ("%SCRIPT_DIR%*.csproj") do (
    set "CSPROJ_FILE=%%f"
)

if "%CSPROJ_FILE%"=="" (
    echo ERROR: csproj file not found in directory "%SCRIPT_DIR%"
    exit /b 1
)

echo Found project: %CSPROJ_FILE%

set "OUTPUT_DIR=%SCRIPT_DIR%publish"

REM Очищаем папку publish перед сборкой
echo Cleaning output directory...
if exist "%OUTPUT_DIR%" (
    rd /s /q "%OUTPUT_DIR%"
)
md "%OUTPUT_DIR%"

echo Output directory: %OUTPUT_DIR%

echo.
echo Starting build...
echo ============================================================

dotnet publish "%CSPROJ_FILE%" --configuration Release --output "%OUTPUT_DIR%"

if %ERRORLEVEL% equ 0 (
    echo.
    echo ============================================================
    echo Build completed successfully!
    echo Output: %OUTPUT_DIR%
    echo ============================================================
    
    REM Показываем размер exe
    for %%f in ("%OUTPUT_DIR%\*.exe") do (
        echo Executable: %%~nxf (%%~zf bytes)
    )
) else (
    echo.
    echo ============================================================
    echo ERROR: Build failed!
    echo ============================================================
    exit /b 1
)

endlocal
