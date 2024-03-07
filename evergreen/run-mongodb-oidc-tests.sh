#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#   OS                                  Operating system, must be set
#   ADMIN_USER                          Admin user, must be set
#   ADMIN_PASSWORD                      Admin password, must be set
#   MONGODB_URI                         Single atlas proxy serverless uri, must be set
#   OIDC_TOKEN_DIR                      Directory to store aws credentials
# Modified/exported environment variables:
#   MONGODB_URI                         MONGODB_URI with embedded admin credentials
#   AWS_WEB_IDENTITY_TOKEN_FILE         Path to the aws credentials file
#   OIDC_TESTS_ENABLED                  Flag to run Oidc tests
#   OIDC_PROVIDER_NAME                  OIDC provider name to be used in tests

############################################
#            Main Program                  #
############################################

if [ -z "$ADMIN_USER" ]; then
  echo "ADMIN_USER should be specified"
  exit 1
fi

if [ -z "$ADMIN_PASSWORD" ]; then
  echo "ADMIN_PASSWORD should be specified"
  exit 1
fi

if [ -z "$MONGODB_URI" ]; then
  echo "MONGODB_URI should be specified"
  exit 1
fi

if [ -z "$OIDC_TOKEN_DIR" ]; then
  echo "OIDC_TOKEN_DIR should be specified"
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

# Make sure DRIVERS_TOOLS is set.
if [ -z "$DRIVERS_TOOLS" ]; then
  echo "Must specify DRIVERS_TOOLS"
  exit 1
fi

# Make the OIDC tokens.
set -x
pushd ${DRIVERS_TOOLS}/.evergreen/auth_oidc
. ./oidc_get_tokens.sh
popd

# Assume "mongodb+srv" protocol
export MONGODB_URI="mongodb+srv://${ADMIN_USER}:${ADMIN_PASSWORD}@${MONGODB_URI}?authSource=admin"
export AWS_WEB_IDENTITY_TOKEN_FILE="$OIDC_TOKEN_DIR/test_user1"
export OIDC_PROVIDER_NAME="aws"
export OIDC_TESTS_ENABLED="true"

if [ "Windows_NT" = "$OS" ]; then
  powershell.exe .\\build.ps1 --target "TestMongoDbOidc"
else
  ./build.sh --target="TestServerless"
fi
