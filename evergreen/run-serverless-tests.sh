#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#   OS                                  Operating system, must be set
#   AUTH                                Authentication flag, must be "auth"
#   SSL                                 TLS connection flag, must be "ssl"
#   COMPRESSOR                          Field level compressor, must be set
#   FRAMEWORK                           Used in build.cake "TestServerless" task, must be set
#   MONGODB_SRV_URI                     Srv URI, produced by create-instance.sh script, must be set
#   MONGODB_URI                         URI with mulpiple mongoses, produced by create-instance.sh script, must be set
# Modified/exported environment variables:
#   MONGODB_URI_WITH_MULTIPLE_MONGOSES  MONGODB_URI with auth details and TLS and compressor parameters
#   MONGODB_URI                         MONGODB_SRV_URI with auth details and TLS and compressor parameters
#   SERVERLESS                          Flag for tests to indicate the test suite

############################################
#            Main Program                  #
############################################

if [[ "$AUTH" != "auth" ]]; then
  echo "Serverless tests require AUTH to be enabled"
  exit 1
fi

if [[ "$SSL" != "ssl" ]]; then
  echo "Serverless tests require SSL to be enabled"
  exit 1
fi

if [ -z "$COMPRESSOR" ]; then
  echo "Serverless tests require COMPRESSOR to be configured"
  exit 1
fi

if [ -z "$FRAMEWORK" ]; then
  echo "Serverless tests require FRAMEWORK to be configured"
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

MONGODB_URI_SPLIT=(${MONGODB_URI//,/ })
export MONGODB_URI_WITH_MULTIPLE_MONGOSES="${MONGODB_URI:0:10}${SERVERLESS_ATLAS_USER}:${SERVERLESS_ATLAS_PASSWORD}@${MONGODB_URI:10}/?tls=true&authSource=admin&compressors=$COMPRESSOR"
export MONGODB_URI="${MONGODB_URI_SPLIT[0]:0:10}${SERVERLESS_ATLAS_USER}:${SERVERLESS_ATLAS_PASSWORD}@${MONGODB_URI_SPLIT[0]:10}/?tls=true&authSource=admin&compressors=$COMPRESSOR"
export SERVERLESS=true

if [ "Windows_NT" = "$OS" ]; then
  powershell.exe .\\build.ps1 --target TestServerless
else
  ./build.sh --target=TestServerless
fi
