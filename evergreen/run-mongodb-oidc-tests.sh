#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

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

# Make the OIDC tokens.
set -x
OIDC_ENV=${OIDC_ENV:-"test"}

if [ $OIDC_ENV == "test" ]; then
  # Make sure DRIVERS_TOOLS is set.
  if [ -z "$DRIVERS_TOOLS" ]; then
    echo "Must specify DRIVERS_TOOLS"
    exit 1
  fi

  source ${DRIVERS_TOOLS}/.evergreen/auth_oidc/secrets-export.sh
  if [[ "$MONGODB_URI" =~ ^mongodb:.* ]]; then
    MONGODB_URI="mongodb://${OIDC_ADMIN_USER}:${OIDC_ADMIN_PWD}@${MONGODB_URI:10}&authSource=admin"
  elif [[ "$MONGODB_URI" =~ ^mongodb\+srv:.* ]]; then
    MONGODB_URI="mongodb+srv://${OIDC_ADMIN_USER}:${OIDC_ADMIN_PWD}@${MONGODB_URI:14}&authSource=admin"
  else
      echo "Unexpected MONGODB_URI format: $MONGODB_URI"
      exit 1
  fi
elif [ $OIDC_ENV == "azure" ]; then
  source ./env.sh
else
  echo "Unrecognized OIDC_ENV $OIDC_ENV"
  exit 1
fi

export OIDC_ENV=$OIDC_ENV
export MONGODB_URI=$MONGODB_URI

if [ "Windows_NT" = "$OS" ]; then
  powershell.exe .\\build.ps1 --target "TestMongoDbOidc"
else
  ./build.sh --target="TestMongoDbOidc"
fi
