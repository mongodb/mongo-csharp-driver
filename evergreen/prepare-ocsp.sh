#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

cd ${DRIVERS_TOOLS}/.evergreen/ocsp

echo "Preparing Python env for OCSP tests"

if [ "Windows_NT" = "$OS" ]; then # Magic variable in cygwin
  /cygdrive/c/python/python38/python.exe -m venv ./venv
  ./venv/Scripts/pip3 install -r ${DRIVERS_TOOLS}/.evergreen/ocsp/mock-ocsp-responder-requirements.txt
else
  echo "$0 needs to be updated to run on non-Windows platforms"
  # Need to ensure on Linux python is installed in the correct place and visible to the script.
  # https://jira.mongodb.org/browse/CSHARP-3255
  # /opt/python/2.7/bin/python -m venv ./venv
  # ./venv/Scripts/pip3 install -r ${DRIVERS_TOOLS}/.evergreen/ocsp/mock-ocsp-responder-requirements.txt
fi
