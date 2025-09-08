#!/usr/bin/env bash

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

# The proxy server processes have almost certainly already been killed by the evergreen process cleaning though.
# This is just to be sure it already happened and delete the file containing the saved PIDs.

echo "Attempting to kill proxy servers if present and deleting PID file."
PID_FILE="socks5_pids.txt"

if [[ ! -f "$PID_FILE" ]]; then
  echo "No PID file found ($PID_FILE)"
  exit 0
fi

cat "$PID_FILE" | while read -r pid; do
  if [[ -n "$pid" ]]; then
    if [[ "$OS" =~ Windows|windows ]]; then
      powershell -NoProfile -Command "Stop-Process -Id $pid -Force" 2>$null || \
        echo "PID $pid already gone"
    else
      kill "$pid" 2>/dev/null || echo "PID $pid already gone"
    fi
  fi
done

rm -f "$PID_FILE"