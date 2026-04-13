@echo off
chcp 65001 >nul
title Clean Temporary Project Files

echo ========================================================
echo     Clean temporary files for UE5 project
echo ========================================================
echo.

REM Удаление папок плагина VoskPlugin57, но сохраняем Plugins\VoskPlugin57\Binaries\Win64\Models
if exist "Plugins\VoskPlugin57\Binaries" (
    echo Cleaning Plugins\VoskPlugin57\Binaries (preserving Win64\Models)...
    pushd "Plugins\VoskPlugin57\Binaries"

    rem Удаляем все вложенные папки, кроме Win64. Для Win64 — удаляем всё кроме Models.
    for /d %%D in (*) do (
        if /I "%%~nxD"=="Win64" (
            echo Preserving Win64 directory structure, cleaning its contents except Models...
            pushd "%%D"
            for /d %%S in (*) do (
                if /I "%%~nxS"=="Models" (
                    echo Skipping %%D\%%S
                ) else (
                    echo Removing %%D\%%S...
                    rmdir /s /q "%%S"
                )
            )
            rem Удаляем файлы, находящиеся в Plugins\VoskPlugin57\Binaries\Win64 (не затрагивая папку Models)
            for %%F in (*) do (
                if exist "%%F" (
                    echo Deleting file %%D\%%F...
                    del /q "%%F"
                )
            )
            popd
        ) else (
            echo Removing directory %%D...
            rmdir /s /q "%%D"
        )
    )

    rem Удаляем файлы в корне Plugins\VoskPlugin57\Binaries
    for %%F in (*) do (
        if exist "%%F" (
            echo Deleting file %%F...
            del /q "%%F"
        )
    )

    popd
)

if exist "Plugins\VoskPlugin57\Intermediate" (
    echo Removing Plugins\VoskPlugin57\Intermediate...
    rmdir /s /q "Plugins\VoskPlugin57\Intermediate"
)

REM Удаление общих папок проекта
if exist "Intermediate" (
    echo Removing Intermediate...
    rmdir /s /q "Intermediate"
)

if exist "DerivedDataCache" (
    echo Removing DerivedDataCache...
    rmdir /s /q "DerivedDataCache"
)

if exist "Binaries" (
    echo Removing Binaries...
    rmdir /s /q "Binaries"
)

REM Удаление .sln файлов
echo Removing .sln files...
del /q *.sln 2>nul

echo.
echo [DONE] Cleanup finished.
pause