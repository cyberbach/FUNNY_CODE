@echo off
setlocal

if not exist "logs" mkdir logs

set "LOG_FILE=logs\server_%DATE:~-4%%DATE:~3,2%%DATE:~0,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%.log"
set "LOG_FILE=%LOG_FILE: =0%"

echo Starting server...
echo Log file: %LOG_FILE%

node server.js > "%LOG_FILE%" 2>&1

endlocal
