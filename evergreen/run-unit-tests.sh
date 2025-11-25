#!/usr/bin/env bash
set -o errexit  # Exit the script with error if any of the commands fail

FRAMEWORK=${FRAMEWORK:-net6.0}

if [ "$FRAMEWORK" = "netstandard2.1" ]; then
  FRAMEWORK="netcoreapp3.1"
fi

export ADD_NET10_TFM="1" # Remove after cake removal
./evergreen/compile-sources.sh
dotnet test -c Release --no-build --filter "Category!=Integration"  -f "$FRAMEWORK" --results-directory ./build/test-results --logger  "junit;verbosity=detailed;LogFileName=TEST-{assembly}.xml;FailureBodyFormat=Verbose" --logger "console;verbosity=detailed"
