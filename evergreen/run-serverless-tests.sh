#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#   AUTH                                Authentication flag, must be "auth"
#   FRAMEWORK                           Used in build.cake "TestServerless" task, must be set
#   OS                                  Operating system, must be set
#   SERVERLESS_ATLAS_USER               Authentication user, must be set
#   SERVERLESS_ATLAS_PASSWORD           Authentiction password, must be set
#   SERVERLESS_URI                      Single atlas proxy serverless uri, must be set
#   SSL                                 TLS connection flag, must be "ssl"
#   CRYPT_SHARED_LIB_PATH               The path to crypt_shared library
# Modified/exported environment variables:
#   MONGODB_URI                         MONGODB_URI for single host with auth details and TLS and compressor parameters
#   MONGODB_URI_WITH_MULTIPLE_MONGOSES  MONGODB_URI with auth details and TLS and compressor parameters
#   SERVERLESS                          Flag for the tests, since there's no other way to determine if running serverless

############################################
#            Main Program                  #
############################################

echo "CRYPT_SHARED_LIB_PATH: ${CRYPT_SHARED_LIB_PATH}"

if [[ "$AUTH" != "auth" ]]; then
  echo "Serverless tests require AUTH to be enabled"
  exit 1
fi

if [ -z "$FRAMEWORK" ]; then
  echo "Serverless tests require FRAMEWORK to be configured"
  exit 1
fi

if [[ "$SSL" != "ssl" ]]; then
  echo "Serverless tests require SSL to be enabled"
  exit 1
fi

if [ "$OS" = "Windows_NT" ]; then
  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    setx $var z:\\data\\tmp
    export $var=z:\\data\\tmp
  done
else
  for var in TMP TEMP NUGET_PACKAGES NUGET_HTTP_CACHE_PATH APPDATA; do
    export $var=/data/tmp;
  done
fi

# Assume "mongodb+srv" protocol
export MONGODB_URI="mongodb+srv://${SERVERLESS_ATLAS_USER}:${SERVERLESS_ATLAS_PASSWORD}@${SERVERLESS_URI:14}"
export SERVERLESS="true"

if [ "Windows_NT" = "$OS" ]; then
  powershell.exe .\\build.ps1 --target "TestServerless${FRAMEWORK}"
else
  ./build.sh --target="TestServerless${FRAMEWORK}"
fi
