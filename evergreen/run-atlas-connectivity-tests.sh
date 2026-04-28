#!/usr/bin/env bash

set -o errexit  # Exit the script with error if any of the commands fail

############################################
#            Main Program                  #
############################################

export ATLAS_CONNECTIVITY_TESTS_ENABLED=true

# Provision the correct connection string and set up SSL if needed
./evergreen/compile-sources.sh
TEST_PROJECT_PATH=./tests/**/AtlasConnectivity.Tests.csproj ./evergreen/execute-tests.sh
