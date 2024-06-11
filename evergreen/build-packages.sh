#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
# PACKAGE_VERSION

if [ -z "$PACKAGE_VERSION" ]; then
  PACKAGE_VERSION=$(bash ./evergreen/get-version.sh)
  echo Calculated PACKAGE_VERSION value: "$PACKAGE_VERSION"
fi

echo Creating nuget package...

dotnet clean ./CSharpDriver.sln
dotnet pack ./CSharpDriver.sln -o ./artifacts/nuget -c Release -p:Version="$PACKAGE_VERSION" --include-symbols -p:SymbolPackageFormat=snupkg -p:ContinuousIntegrationBuild=true