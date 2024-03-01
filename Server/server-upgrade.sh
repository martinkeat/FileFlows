#!/usr/bin/env bash

if [ "$1" == "mac" ]; then
  # this means it running as a mac .app and we treat it differently
  kill %1
  
  cd ..
  
  rm -rf ../Logs/upgrade.log
  
  echo 'Upgrading FileFlows' >> ..Logs//upgrade.log
  
  # update version file
  echo "$3" > version
  
  rm -rf Server >> ../Logs/upgrade.log
  rm -rf FlowRunner >> ../Logs/upgrade.log
  
  mv Update/FlowRunner FlowRunner >> ../Logs/upgrade.log
  mv Update/Server Server >> ../Logs/upgrade.log
  
  rm -rf Update >> ../Logs/upgrade.log
  
  echo "Launching open -a ""$2""" >> ../Logs/upgrade.log
  
  # Relaunch the macOS .app folder
  open -a "$2"
else
  
  if [ "$1" != "systemd" && "$1" != "docker" ]; then 
    kill %1
  fi

  cd ..
  rm -rf Server
  rm -rf FlowRunner
  rm run-node.sh
  rm run-server.sh
  
  mv Update/FlowRunner FlowRunner
  mv Update/Server Server
  mv Update/run-node.sh run-node.sh
  mv Update/run-server.sh run-server.sh
  
  rm -rf Update
  
  if [ "$1" != "systemd" && "$1" != "docker" ]; then 
    chmod +x run-server.sh
    ./run-server.sh
  fi
fi