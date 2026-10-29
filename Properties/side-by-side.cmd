@echo off
setlocal

rem Hosts a session and joins it from a second window, each filling half the screen, for trying multiplayer locally

set "SCRIPT_DIR=%~dp0"
set "PROJECT_DIR=%SCRIPT_DIR%.."

set "PORT=7777"
set "WIDTH=1280"
set "HEIGHT=1380"

start "Host" "%SCRIPT_DIR%launch.cmd" --path "%PROJECT_DIR%" --windowed --resolution %WIDTH%x%HEIGHT% --position 0,0 ^
	-- --port=%PORT% --name=Host

rem Gives the host a head start, so the client's first attempt finds it listening
timeout /t 2 /nobreak >nul

start "Client" "%SCRIPT_DIR%launch.cmd" --path "%PROJECT_DIR%" --windowed --resolution %WIDTH%x%HEIGHT% ^
	--position %WIDTH%,0 -- --join=127.0.0.1:%PORT% --name=Client
