#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr

# Environment variables used as input:
#   OS                                               The current operating system

echo "Attempt to kill proxy server process if present on ${OS}"
if [[ "$OS" =~ Windows|windows ]]; then
  tasklist -FI "IMAGENAME eq python.exe"
  taskkill -F -FI "IMAGENAME eq python.exe"
  # check that it's actually killed
  tasklist -FI "IMAGENAME eq python.exe"
else
  ps -ax | grep socks5srv.py
  pkill -f 'socks5srv.py' || echo 'socks5srv.py was already killed or not launched'
  # check that it's actually killed
  ps -ax | grep socks5srv.py
fi
