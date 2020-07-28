#!/bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server via PLAIN authentication mechanism

############################################
#            Main Program                  #
############################################

echo "Running GSSAPI authentication tests"
if [ -z ${MONGODB_URI+x} ]; then
    echo "MONGODB_URI is not set";
    exit 1
fi
export MONGODB_URI="${MONGODB_URI}&authSource=\$external"
powershell.exe \
  '$env:EXPLICIT="true";' \
  '.\\build.ps1 -target TestGSSAPIAuthentication'
