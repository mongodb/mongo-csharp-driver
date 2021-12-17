#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr

# Environment variables used as input:
#   OS                                               The current operating system

echo "Attempt to kill mongocryptd process if presented on ${OS}"
if [[ "$OS" =~ Windows|windows ]]; then
  tasklist -FI "IMAGENAME eq mongocryptd.exe"
  taskkill -F -FI "IMAGENAME eq mongocryptd.exe"
  # check that it's actually killed
  tasklist -FI "IMAGENAME eq mongocryptd.exe"
else
  ps -ax | grep mongocryptd
  pkill -f 'mongocryptd' || echo 'mongocryptd was already killed or not launched'
  # check that it's actually killed
  ps -ax | grep mongocryptd 
fi
