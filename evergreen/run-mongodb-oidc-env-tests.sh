#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

DOTNET_SDK_PATH="$(pwd)/.dotnet"

echo "Downloading .NET SDK installer into $DOTNET_SDK_PATH folder..."
curl -Lfo ./dotnet-install.sh https://dot.net/v1/dotnet-install.sh
echo "Installing .NET LTS SDK..."
bash ./dotnet-install.sh --channel 6.0 --install-dir "$DOTNET_SDK_PATH" --no-path
export PATH=$DOTNET_SDK_PATH:$PATH

if [ "$OIDC_ENV" == "azure" ]; then
  source ./env.sh
  TOKEN_RESOURCE="$AZUREOIDC_RESOURCE"
elif [ "$OIDC_ENV" == "gcp" ]; then
  source ./secrets-export.sh
  TOKEN_RESOURCE="$GCPOIDC_AUDIENCE"
else
  echo "Unrecognized OIDC_ENV $OIDC_ENV"
  exit 1
fi

if [[ "$MONGODB_URI" =~ ^mongodb:.* ]]; then
  MONGODB_URI="mongodb://${OIDC_ADMIN_USER}:${OIDC_ADMIN_PWD}@${MONGODB_URI:10}?authSource=admin"
elif [[ "$MONGODB_URI" =~ ^mongodb\+srv:.* ]]; then
  MONGODB_URI="mongodb+srv://${OIDC_ADMIN_USER}:${OIDC_ADMIN_PWD}@${MONGODB_URI:14}?authSource=admin"
else
    echo "Unexpected MONGODB_URI format: $MONGODB_URI"
    exit 1
fi

sleep 60 # sleep for 1 minute to let cluster make the master election

dotnet test --no-build --framework net6.0 --filter Category=MongoDbOidc -e OIDC_ENV="$OIDC_ENV" -e TOKEN_RESOURCE="$TOKEN_RESOURCE" -e MONGODB_URI="$MONGODB_URI" --results-directory ./build/test-results --logger "console;verbosity=detailed" ./tests/**/*.Tests.dll
