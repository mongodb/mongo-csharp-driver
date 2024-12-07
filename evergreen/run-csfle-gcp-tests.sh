#!/bin/bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

# Supported/used environment variables:
#       MONGODB_URI             Set the URI, including an optional username/password to use to connect to the server
############################################
#            Main Program                  #
############################################

echo "Running GCP Credential Acquisition Test"

# fixing https://github.com/dotnet/core/issues/2186#issuecomment-671105420
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export CSFLE_GCP_KMS_TESTS_ENABLED=true

./build.sh --target=TestCsfleWithGcpKms
