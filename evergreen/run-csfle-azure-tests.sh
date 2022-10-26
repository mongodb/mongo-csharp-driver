#!/bin/bash

set -o xtrace
set -o errexit  # Exit the script with error if any of the commands fail

# Environment variables used as input:
#       MONGODB_URI             Set the URI, including an optional username/password to use to connect to the server
#       KEY_NAME                Set azure kms key name
#       KEY_VAULT_ENDPOINT      Set azure kms key vault endpoint
#
# Environment variables produced as output
#       DOTNET_SYSTEM_GLOBALIZATION_INVARIANT   Workaround for the https://github.com/dotnet/core/issues/2186 issue.
#       CSFLE_AZURE_KMS_TESTS_ENABLED  Enable csfle azure kms tests.

############################################
#            Main Program                  #
############################################

echo "Running Azure Credential Acquisition Test"

# fixing https://github.com/dotnet/core/issues/2186#issuecomment-671105420
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export CSFLE_AZURE_KMS_TESTS_ENABLED=true

./build.sh --target=TestCsfleWithAzureKms
