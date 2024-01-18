@echo off

rem Check if the script is already in upgrade mode
if "%FF_UPGRADE_MODE%"=="1" goto :RunUpdate

rem Set the environment variable to indicate upgrade mode
set "FF_UPGRADE_MODE=1"
copy server-upgrade.bat ..\
cd ..
start server-upgrade.bat %1 & exit
GOTO Done

:RunUpdate

rem Reset the environment variable to avoid issues in future runs
set "FF_UPGRADE_MODE="

timeout /t 3
echo Stopping FileFlows Server if running
taskkill /PID %2

echo.
echo Removing previous version
rmdir /q /s Server
rmdir /q /s FlowRunner

echo.
echo Copying Server update files
move Update/FlowRunner FlowRunner
move Update/Server Server
rmdir /q /s Update

echo.
echo Starting FileFlows Server
cd Server
start FileFlows.Server.exe
cd .. 

if exist server-upgrade.bat goto Done
del server-upgrade.bat & exit

:Done
exit
