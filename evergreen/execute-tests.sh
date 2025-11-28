#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#   FRAMEWORK                       Set to specify .NET framework to test against. Values: "Net472", "NetStandard21",
#   TEST_CATEGORY                   Set to specify a test category to filter by.
#   TEST_PROJECT_PATH               Set glob filter to find test projects.

FRAMEWORK=${FRAMEWORK:-}
TEST_CATEGORY=${TEST_CATEGORY:-Integration}
TEST_PROJECT_PATH=${TEST_PROJECT_PATH:-./tests/**/[!Atlas]*.Tests.csproj}

if [ "$FRAMEWORK" = "netstandard2.1" ]; then
  FRAMEWORK="netcoreapp3.1"
fi

FILTER_PARAMETER=""
echo TEST_CATEGORY: ${TEST_CATEGORY}
if [[ -n "${TEST_CATEGORY}" ]]; then
  if [[ "${TEST_CATEGORY}" == "!"* ]]; then
    FILTER_PARAMETER="--filter \"Category!=${TEST_CATEGORY:1}\""
  else
    FILTER_PARAMETER="--filter \"Category=${TEST_CATEGORY}\""
  fi
fi

FRAMEWORK_PARAMETER=""
if [[ -n "${FRAMEWORK}" ]]; then
  FRAMEWORK_PARAMETER="-f \"${FRAMEWORK}\""
fi

for file in $TEST_PROJECT_PATH; do
  dotnet test "${file}" -c Release --no-build ${FILTER_PARAMETER} ${FRAMEWORK_PARAMETER} --results-directory ./build/test-results --logger  "junit;verbosity=detailed;LogFileName=TEST-{assembly}.xml;FailureBodyFormat=Verbose" --logger "console;verbosity=detailed"
done
