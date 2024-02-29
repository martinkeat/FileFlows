#!/usr/bin/env bash

# Change to the current directory

# Change directory to Server and push to directory stack, suppressing output
pushd "$(dirname "$0")/Server" > /dev/null

# Run the .NET Core application in the background, suppressing output
dotnet FileFlows.Server.dll --gui > /dev/null 2>&1 &

# Store the process ID (PID) of the dotnet process
dotnet_pid=$!

# Wait for 5 seconds
sleep 5

# Check if the dotnet process is still running
if ps -p $dotnet_pid > /dev/null; then
    echo "Application started successfully."
else
    echo "Error: Application failed to start."
fi

# Return to the original directory
popd > /dev/null