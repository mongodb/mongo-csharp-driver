#!/usr/bin/env bash

# Don't trace since the URI contains a password that shouldn't show up in the logs
set -o errexit # Exit the script with error if any of the commands fail

export FRAMEWORK=net6.0
. ./evergreen/install-dotnet.sh

if [ "$OIDC_ENV" == "azure" ]; then
  source ./env.sh
  TOKEN_RESOURCE="$AZUREOIDC_RESOURCE"
elif [ "$OIDC_ENV" == "gcp" ]; then
  source ./secrets-export.sh
  TOKEN_RESOURCE="$GCPOIDC_AUDIENCE"
elif [ "$OIDC_ENV" == "k8s" ]; then
  source ./secrets-export.sh
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

# need set DOTNET_SYSTEM_GLOBALIZATION_INVARIANT to avoid "Couldn't find a valid ICU package installed on the system." error
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

dotnet test --no-build --framework net6.0 --filter Category=MongoDbOidc -e OIDC_ENV="$OIDC_ENV" -e TOKEN_RESOURCE="$TOKEN_RESOURCE" -e MONGODB_URI="$MONGODB_URI" --results-directory ./build/test-results --logger "console;verbosity=detailed" ./tests/**/*.Tests.dll
