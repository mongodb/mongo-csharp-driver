#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

DOTNET_SDK_PATH="$(pwd)/.dotnet"

echo "Downloading .NET SDK installer into $DOTNET_SDK_PATH folder..."
curl -Lfo ./dotnet-install.sh https://dot.net/v1/dotnet-install.sh
echo "Installing .NET LTS SDK..."
bash ./dotnet-install.sh --channel 6.0 --install-dir "$DOTNET_SDK_PATH" --no-path
export PATH=$PATH:$DOTNET_SDK_PATH

echo "test variables"

source ./env.sh
cat ./env.sh

MONGODB_URI="mongodb://${OIDC_ADMIN_USER}:${OIDC_ADMIN_PWD}@${MONGODB_URI:10}?authSource=admin"

echo "Final MongoUri:"
echo $MONGODB_URI

dotnet test --no-build --framework net6.0 --filter Category=MongoDbOidc -e OIDC_ENV=azure -e TOKEN_RESOURCE="${AZUREOIDC_RESOURCE}" -e MONGODB_URI="${MONGODB_URI}" --results-directory ./build/test-results --logger "console;verbosity=detailed" ./tests/MongoDB.Driver.Tests/bin/Debug/net6.0/MongoDB.Driver.Tests.dll
