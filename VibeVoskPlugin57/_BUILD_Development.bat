@echo off
echo Building VoskPlugin57 for Development Win64...

set UE_ROOT=
for /f "tokens=2*" %%a in ('reg query "HKLM\SOFTWARE\EpicGames\Unreal Engine\5.7" /v "InstalledDirectory" 2^>nul') do set UE_ROOT=%%b
if not defined UE_ROOT (
  echo Unreal Engine 5.7 not found. Set UE_ROOT environment variable.
  pause
  exit /b 1
)

set PLUGIN_PATH=%~dp0Plugins\VoskPlugin57\VoskPlugin57.uplugin
set OUTPUT_PATH=%~dp0Plugins\VoskPlugin57\Built

"%UE_ROOT%\Engine\Build\BatchFiles\RunUAT.bat" BuildPlugin ^
  -Plugin="%PLUGIN_PATH%" ^
  -Package="%OUTPUT_PATH%" ^
  -Rocket ^
  -TargetPlatforms=Win64 ^
  -TargetConfigurations=Development > build_log.txt 2>&1

if %errorlevel% neq 0 (
  echo Build failed! Opening log...
  start build_log.txt
  pause
  exit /b %errorlevel%
)

echo Build completed successfully.
pause