#!/usr/bin/env bash

SMOKE_TESTS_PROJECT="./tests/SmokeTests/MongoDB.Driver.SmokeTests.Sdk/MongoDB.Driver.SmokeTests.Sdk.csproj"

DRIVER_PACKAGE_VERSION="$1"
if [ -z "$DRIVER_PACKAGE_VERSION" ]; then
  echo "Driver package version should be provided."
  exit 1
fi

. ./evergreen/append-myget-package-source.sh

export DRIVER_PACKAGE_VERSION="${DRIVER_PACKAGE_VERSION}"
. ./evergreen/compile-sources.sh "$SMOKE_TESTS_PROJECT"

dotnet test "$SMOKE_TESTS_PROJECT" -c Release --no-build -f "$FRAMEWORK" --results-directory ./build/test-results --logger  "junit;verbosity=detailed;LogFileName=TEST-{assembly}.xml;FailureBodyFormat=Verbose" --logger "console;verbosity=detailed"
