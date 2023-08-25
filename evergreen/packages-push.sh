#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail
set +o xtrace # Disable tracing.

if [ -z "$PACKAGES_SOURCE" ]; then
  echo "PACKAGES_SOURCE variable should be set"
  exit 1
fi

if [ -z "$PACKAGES_SOURCE_KEY" ]; then
  echo "PACKAGES_SOURCE_KEY variable should be set"
  exit 1
fi

if [ -z "$PACKAGE_VERSION" ]; then
  echo "PACKAGE_VERSION variable should be set"
  exit 1
fi

dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./build/nuget/MongoDB.Bson."$PACKAGE_VERSION".nupkg
dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./build/nuget/MongoDB.Driver.Core."$PACKAGE_VERSION".nupkg
dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./build/nuget/MongoDB.Driver."$PACKAGE_VERSION".nupkg
dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./build/nuget/MongoDB.Driver.GridFS."$PACKAGE_VERSION".nupkg
dotnet nuget push --source "$PACKAGES_SOURCE" --api-key "$PACKAGES_SOURCE_KEY" ./build/nuget/mongocsharpdriver."$PACKAGE_VERSION".nupkg


