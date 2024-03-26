@echo off

move server-upgrade-actual.bat ..\server-upgrade.bat
cd ..
start server-upgrade.bat "UPDATE" %1 & exit