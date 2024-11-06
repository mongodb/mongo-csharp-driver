#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
# PACKAGE_VERSION

if [ -z "$PACKAGE_VERSION" ]; then
  PACKAGE_VERSION=$(bash ./evergreen/get-version.sh)
  echo Calculated PACKAGE_VERSION value: "$PACKAGE_VERSION"
fi

BASELINE_VERSION="${PACKAGE_VERSION%%.*}.0.0"
if [ "$PACKAGE_VERSION" == "$BASELINE_VERSION" ]; then
  echo "Skipping package validation for major release."
  exit 0
fi

echo "Installing package validation tool."
dotnet new tool-manifest --force
dotnet tool install Microsoft.DotNet.ApiCompat.Tool --version 8.0.* --local

EXIT_CODE=0
mkdir -p ./artifacts/apicompat/
source ./evergreen/packages.sh
for package in ${PACKAGES[*]}; do
  echo "Validating ${package}:${PACKAGE_VERSION} for compatibility with ${BASELINE_VERSION}"

  curl -L -o "./artifacts/nuget/${package}.${BASELINE_VERSION}.nupkg" "https://www.nuget.org/api/v2/package/${package}/${BASELINE_VERSION}"
  # run apicompat tool and redirect stderr to file as apicompat reports problems as error output.
  dotnet tool run apicompat package "./artifacts/nuget/${package}.${PACKAGE_VERSION}.nupkg" --baseline-package "./artifacts/nuget/${package}.${BASELINE_VERSION}.nupkg" --enable-rule-cannot-change-parameter-name 2> "./artifacts/apicompat/${package}.${PACKAGE_VERSION}.txt"
  if [ -s "./artifacts/apicompat/${package}.${PACKAGE_VERSION}.txt" ] ; then
    EXIT_CODE=1
  else
    # delete file if empty (no compatibility issues detected)
    rm "./artifacts/apicompat/${package}.${PACKAGE_VERSION}.txt"
  fi
done

exit $EXIT_CODE
