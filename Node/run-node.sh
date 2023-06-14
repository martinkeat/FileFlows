#!/usr/bin/env bash

(cd Node && nohup dotnet FileFlows.Node.dll --no-gui >/dev/null 2>&1 & )