#!/bin/bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o xtrace   # Write all commands first to stderr
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       FRAMEWORK                   Set target framework to test against
#       MONGODB_URI                 Set the suggested connection MONGODB_URI (including credentials and topology info)
#       KMS_TLS_ERROR_TYPE          Either "expired" or "invalidHostname"

############################################
#            Main Program                  #
############################################

echo "Running KMS TLS tests"

echo "FRAMEWORK: ${FRAMEWORK}"
echo "KMS_TLS_ERROR_TYPE: ${KMS_TLS_ERROR_TYPE}"

if [ "Windows_NT" = "$OS" ]; then
  powershell.exe \
  '.\build.ps1 --target' "TestCsfleKmsTls${FRAMEWORK}"
else
  ./build.sh --target="TestCsfleKmsTls${FRAMEWORK}"
fi
