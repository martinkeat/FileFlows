#!/usr/bin/env bash

if [ "$1" == "mac" ]; then
  # this means it running as a mac .app and we treat it differently
  kill "$3"
  
  cd ..
  
  rm -rf upgrade.log
  
  echo 'Upgrading FileFlows Node' >> upgrade.log
  
  echo "--------------------------------------------" > upgrade.log
  for arg in "$@"; do
      echo "$arg" >> upgrade.log
  done
  echo "--------------------------------------------" > upgrade.log
  
  # update version file
  echo "$3" > version.txt
  
  rm -rf Node >> upgrade.log
  rm -rf FlowRunner >> upgrade.log
  
  mv NodeUpdate/FlowRunner FlowRunner >> upgrade.log
  mv NodeUpdate/Node Node >> upgrade.log
  
  echo 'Removing Update folder' >> upgrade.log
  rm -rf NodeUpdate >> upgrade.log
  
  echo "Launching open -a ""$2""" >> upgrade.log
  
  # Relaunch the macOS .app folder
  # Schedule the application launch using the 'at' command
  (sleep 5 && open -ga "$2") &

  # Exit the script
  exit 0
  
else
    
  if [ "$1" != "systemd" && "$1" != "docker" ]; then 
    kill %1
  fi
  
  cd ..
  rm -rf Node
  rm -rf FlowRunner
  rm run-node.sh
  
  mv NodeUpdate/FlowRunner FlowRunner
  mv NodeUpdate/Node Node
  mv NodeUpdate/run-node.sh run-node.sh
  
  rm -rf NodeUpdate
  
  if [[ "$1" != "systemd" && "$1" != "docker" ]]; then 
    chmod +x run-node.sh
    ./run-node.sh
  fi
fi