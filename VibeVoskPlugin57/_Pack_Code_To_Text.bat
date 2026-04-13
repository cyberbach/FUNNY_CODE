@echo off
set OUTPUT_FILE=VoskPlugin57_Code.txt
if exist %OUTPUT_FILE% del %OUTPUT_FILE%

for /r "Plugins\VoskPlugin57" %%f in (*.cpp *.h *.cs *.uplugin) do (
  echo ===== %%f ===== >> %OUTPUT_FILE%
  type "%%f" >> %OUTPUT_FILE%
  echo. >> %OUTPUT_FILE%
)

echo Done. Code packed to %OUTPUT_FILE%