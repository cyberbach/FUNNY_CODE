@echo off
chcp 65001 >nul
title Build Unreal Project

echo ========================================================
echo     Build Unreal Engine project
echo ========================================================
echo.

:: ----- 1. Находим .uproject в текущей папке -----
set UPROJECT_FILE=
for %%f in ("%~dp0*.uproject") do set UPROJECT_FILE=%%f
if not defined UPROJECT_FILE (
    echo [ERROR] No .uproject file found in folder %~dp0
    pause
    exit /b 1
)
echo [1] Project: %UPROJECT_FILE%

:: ----- 2. Запускаем сборку -----
call "D:\Epic Games\UE_5.7\Engine\Build\BatchFiles\Build.bat" MonstersOnTitanEditor Win64 Development -Project=%UPROJECT_FILE% -WaitMutex

:: ----- 3. Открываем лог-файл UnrealBuildTool -----
echo [2] Opening log: C:\Users\AndreyWuzHere\AppData\Local\UnrealBuildTool\Log.txt
start "" "C:\Users\AndreyWuzHere\AppData\Local\UnrealBuildTool\Log.txt"

echo.
echo [DONE] Build process completed.
REM pause