@echo off

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