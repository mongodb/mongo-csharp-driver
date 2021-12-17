#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including username/password to use to connect to the server via PLAIN authentication mechanism

############################################
#            Main Program                  #
############################################

echo "Running PLAIN authentication tests"

if [ -z ${MONGODB_URI+x} ]; then
    echo "MONGODB_URI is not set";
    exit 1
fi
export MONGODB_URI="${MONGODB_URI}"
export PLAIN_AUTH_TESTS_ENABLED=true

if [[ "$OS" =~ Windows|windows ]]; then
  powershell.exe \
    '.\build.ps1 --target TestPlainAuthentication'
else
  ./build.sh --target=TestPlainAuthentication
fi
