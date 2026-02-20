@echo off
REM ============================================================
REM Batch-файл для сборки shipping-версии .NET проекта
REM ============================================================

REM Включаем отложенное расширение переменных для работы с переменными внутри циклов
setlocal enabledelayedexpansion

REM Получаем путь к директории, где находится этот бат-файл
set "SCRIPT_DIR=%~dp0"

REM Ищем csproj-файл в текущей директории
set "CSPROJ_FILE="

REM Цикл по всем csproj-файлам в директории скрипта
for %%f in ("%SCRIPT_DIR%*.csproj") do (
    set "CSPROJ_FILE=%%f"
)

REM Проверяем, найден ли csproj-файл
if "%CSPROJ_FILE%"=="" (
    echo ERROR: csproj file not found in directory "%SCRIPT_DIR%"
    exit /b 1
)

echo Found project: %CSPROJ_FILE%

REM Определяем выходную директорию (папка publish рядом с бат-файлом)
set "OUTPUT_DIR=%SCRIPT_DIR%publish"

echo Output directory: %OUTPUT_DIR%

REM Выполняем сборку shipping-версии
echo.
echo Starting build...
echo ============================================================

dotnet publish "%CSPROJ_FILE%" --configuration Release --output "%OUTPUT_DIR%"

REM Проверяем результат сборки
if %ERRORLEVEL% equ 0 (
    echo.
    echo ============================================================
    echo Build completed successfully!
    echo Output: %OUTPUT_DIR%
    echo ============================================================
) else (
    echo.
    echo ============================================================
    echo ERROR: Build failed!
    echo ============================================================
    exit /b 1
)

endlocal
