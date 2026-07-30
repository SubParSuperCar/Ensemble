@echo off
setlocal

echo Runtime context: Windows NT (CMD)

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%.."
set "LOCAL_BIN=%PROJECT_DIR%\bin"

set "GD_CANDIDATES=godot.exe godot4.exe godot-mono.exe"

for %%G in (%GD_CANDIDATES%) do (
    if exist "%LOCAL_BIN%\%%G" (
        echo Found via local bin: "%LOCAL_BIN%\%%G"
        "%LOCAL_BIN%\%%G" %*
        exit /b
    )
)

for %%G in (%GD_CANDIDATES%) do (
    for /f "delims=" %%P in ('where %%G 2^>nul') do (
        echo Found via PATH (%%G): %%P
        "%%P" %*
        exit /b
    )
)

echo Godot not found in local bin or via PATH.
exit /b 1
