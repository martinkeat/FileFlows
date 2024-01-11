@echo off

IF "%1" == "UPDATE" GOTO RunUpdate
    echo Launching from Subdirectory %1
    copy node-upgrade.bat ..\node-upgrade.bat
    start /D "..\" node-upgrade.bat UPDATE %1 & exit
GOTO Done


:RunUpdate

    echo Running Update
    timeout /t 3
    echo Stopping FileFlows Node if running
    taskkill /PID %2 2>NUL
    
    echo.
    echo Removing previous version
    rmdir /q /s "%~dp0%Node"
    rmdir /q /s "%~dp0%FlowRunner"
    
    echo.
    echo Copying Node update files
    if exist NodeUpdate/FlowRunner/FlowRunner (
        move NodeUpdate/FlowRunner/FlowRunner FlowRunner
    ) else (
        move NodeUpdate/FlowRunner FlowRunner
    )
    if exist NodeUpdate/Node/Node (
        move NodeUpdate/Node/Node Node
    ) else ( 
        move NodeUpdate/Node Node
    )
    rmdir /q /s "%~dp0%NodeUpdate"
    
    echo.
    echo Starting FileFlows Node
    start /D "%~dp0%Node" FileFlows.Node.exe
    
    if exist node-upgrade.bat goto Done
    del node-upgrade.bat & exit

:Done
exit
