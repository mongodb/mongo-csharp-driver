#!/usr/bin/env bash

set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with an error if any of the commands fail

cd ${DRIVERS_TOOLS}/.evergreen/ocsp

echo "Preparing Python env for OCSP tests"

if [ "$OSTYPE" = "cygwin" ]; then
  /cygdrive/c/python/python38/python.exe -m venv ./venv
  ./venv/Scripts/pip3 install -r ${DRIVERS_TOOLS}/.evergreen/ocsp/mock-ocsp-responder-requirements.txt
else
  # Need to ensure on Linux python is installed in the correct place and visible to the script.
  # /opt/python/2.7/bin/python -m venv ./venv
  # ./venv/Scripts/pip3 install -r ${DRIVERS_TOOLS}/.evergreen/ocsp/mock-ocsp-responder-requirements.txt
fi
