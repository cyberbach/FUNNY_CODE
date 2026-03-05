@echo off
cd /d "%~dp0"
dotnet publish MP3RandomRenamer.csproj -c Release -o publish
echo.
echo Build complete! Check publish folder.
pause
